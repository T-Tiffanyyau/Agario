/// <summary>
/// Author:    Man Wai Lam & Tiffany Yau
/// Date:      26 Apr 2023
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and Tiffany Yau - This work may not 
///            be copied for use in Academic Coursework.
///
/// We, Man Wai Lam & Tiffany Yau, certify that I wrote this code from scratch and
/// did not copy it in part or whole from another source.  All 
/// references used in the completion of the assignments are cited 
/// in my README file.
///
/// File Contents
///
///    This class creates the WebServer and also the html pages for checking game status.
/// 
/// </summary>
using Communications;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Web;
using System.Globalization;
using System.Data;

namespace WebServer
{
    /// <summary>
    /// Code for webpage server.
    /// </summary>
    class WebServer
    {
        /// <summary>
        /// keep track of how many requests have come in.  Just used
        /// for display purposes.
        /// </summary>
        static private int counter = 1;
        public static readonly string connectionString;
        private static Dictionary<Networking, StringBuilder> clientRequestData = new Dictionary<Networking, StringBuilder>();
        int port = 11001;
        private static ILogger logger = NullLogger.Instance;
        static Networking server = new Networking(logger: logger, OnClientConnect, OnDisconnect, onMessage, '\n');
        private static Dictionary<long, PlayerData> playerDataDictionary;
        private static Dictionary<int, Dictionary<long, PlayerData>> GameIDWorld = new Dictionary<int, Dictionary<long, PlayerData>>();
        private static int CurrentGameID;
        private static readonly object playerDataLock = new object();
        private static readonly object AddplayerDataLock = new object();

        /// <summary>
        /// The constructor of the webserver class.
        /// </summary>
        static WebServer()
        {
            var builder = new ConfigurationBuilder();

            builder.AddUserSecrets<WebServer>();
            IConfigurationRoot Configuration = builder.Build();
            var SelectedSecrets = Configuration.GetSection("WebServerSecrets");

            connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = SelectedSecrets["DataSource"],
                InitialCatalog = SelectedSecrets["InitialCatalog"],
                UserID = SelectedSecrets["UserID"],
                Password = SelectedSecrets["Password"],
                Encrypt = false
            }.ConnectionString;

            playerDataDictionary = new Dictionary<long, PlayerData>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand("IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AgarioGame') SELECT 1 ELSE SELECT 0", connection);

                bool tableExists = (int)command.ExecuteScalar() == 1;

                if (tableExists)
                {
                    command = new SqlCommand("SELECT MAX(Game_ID) FROM AgarioGame", connection);
                    object result = command.ExecuteScalar();
                    if (result == DBNull.Value)
                    {
                        CurrentGameID = 1;
                    }
                    else
                    {
                        CurrentGameID = (int)result + 1;
                    }
                }
                else
                {
                    // Table doesn't exist, set CurrentGameID to 1 or handle the situation accordingly.
                    CurrentGameID = 1;
                }

                connection.Close();
            }

            GameIDWorld.Add(CurrentGameID, playerDataDictionary);
        }

        /// <summary>
        /// The main method of the webserver class.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Server Start");
            server.WaitForClients(11001, true);
            Console.ReadLine();
        }

        /// <summary>
        /// Basic connect handler - i.e., a browser has connected!
        /// Print an information message
        /// </summary>
        /// <param name="channel"> the Networking connection</param>
        internal static void OnClientConnect(Networking channel)
        {
            Console.WriteLine("Connected");
        }

        /// <summary>
        /// Create the HTTP response header, containing items such as
        /// the "HTTP/1.1 200 OK" line.
        /// 
        /// See: https://www.tutorialspoint.com/http/http_responses.htm
        /// 
        /// Warning, don't forget that there have to be new lines at the
        /// end of this message!
        /// </summary>
        /// <param name="length"> how big a message are we sending</param>
        /// <param name="type"> usually html, but could be css</param>
        /// <returns>returns a string with the response header</returns>
        private static string BuildHTTPResponseHeader(int length, string type = "text/html")
        {
            return $"HTTP/1.1 200 OK\r\nContent-Type: {type}\r\nContent-Length: {length}\r\n\r\n";
        }

        /// <summary>
        ///   Create a web page!  The body of the returned message is the web page
        ///   "code" itself. Usually this would start with the doctype tag followed by the HTML element.  Take a look at:
        ///   https://www.sitepoint.com/a-basic-html5-template/
        /// </summary>
        /// <returns> A string the represents a web page.</returns>
        private static string BuildHTTPBody(string pageType = "home")
        {
            string htmlContent = "";
            string cssLink = "<link rel='stylesheet' href='/css/styles.css'>";
            if (pageType == "home")
            {
                htmlContent = $@"
    {cssLink}<div class='centered-content'>
    <h1>Welcome to our Agario Scoreboard!</h1>
    <p>Click on the buttons below to access different pages:</p>
    <div>
        <a href='/highscore' class='btn'>Highscore Page</a>
        <a href='/scores/name' class='btn'>Get User Scores</a>
        <a href='/fancy' class='btn'>Fancy Page</a>
        <a href='/create' class='btn'>Create DB Tables </a>
    </div>
</div>";

            }
            else if (pageType == "highscore")
            {
                DataTable highscoreData = FetchHighscoreData();
                StringBuilder highscoreTable = new StringBuilder();

                highscoreTable.Append("<table><thead><tr><th>Game ID</th><th>Player ID</th><th>Player Name</th><th>Mass</th><th>Currently Playing</th></tr></thead><tbody>");

                foreach (DataRow row in highscoreData.Rows)
                {
                    highscoreTable.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>", row["Game_ID"], row["Player_ID"], row["Player_Name"], row["Mass"], row["Alive"]);
                }

                highscoreTable.Append("</tbody></table>");

                htmlContent = $"<h1>Highscore Page</h1>{cssLink}{highscoreTable}";
            }
            else if (pageType == "user_scores")
            {
                DataTable userScoresData = FetchUserScoresData();
                StringBuilder userScoresTable = new StringBuilder();

                userScoresTable.Append("<table><thead><tr><th>Game ID</th><th>Player ID</th><th>Player Name</th><th>Mass</th><th>Game Time Lasted In Seconds</th><th>StartTime</th><th>DeadTime</th></tr></thead><tbody>");

                foreach (DataRow row in userScoresData.Rows)
                {
                    userScoresTable.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>", row["Game_ID"], row["Player_ID"], row["Player_Name"], row["HighestMass"], row["GameTimeLastedInSeconds"], row["StartTime"], row["DeadTime"]);
                }

                userScoresTable.Append("</tbody></table>");

                htmlContent = $"<h1>User Scores Page</h1><p>If want to check only one player can type the name on the website</p>{cssLink}{userScoresTable}";
            }
            return htmlContent;
        }

        /// <summary>
        /// Builds the highscore page.
        /// </summary>
        /// <returns> the highscore page string</returns>
        private static string BuildHighscorePage()
        {
            string message = BuildHTTPBody("highscore");
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Builds the BuildUserScores page.
        /// </summary>
        /// <returns> the BuildUserScores page string</returns>
        private static string BuildUserScoresPage()
        {
            string message = BuildHTTPBody("user_scores");
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Create a response message string to send back to the connecting
        /// program (i.e., the web browser).  The string is of the form:
        /// 
        ///   HTTP Header
        ///   [new line]
        ///   HTTP Body
        ///  
        ///  The Header must follow the header protocol.
        ///  The body should follow the HTML doc protocol.
        /// </summary>
        /// <returns> the complete HTTP response</returns>
        private static string BuildMainPage()
        {
            string message = BuildHTTPBody();
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Fetches the user score data
        /// </summary>
        /// <returns></returns>
        private static DataTable FetchUserScoresData()
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"SELECT Game_ID, Player_ID, Player_Name, HighestMass, GameTimeLastedInSeconds, StartTime, DeadTime
                             FROM AgarioPlayerDetailedData
                             ORDER BY Game_ID ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }

            return dataTable;
        }

        /// <summary>
        /// fetches the high score data
        /// </summary>
        /// <returns></returns>
        private static DataTable FetchHighscoreData()
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Add 'ORDER BY Game_ID ASC' at the end of the query
                    string query = @"SELECT Game_ID, Player_ID, Player_Name, Mass, Alive
                             FROM AlivePlayersRank
                             ORDER BY Game_ID ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }

            return dataTable;
        }

        /// <summary>
        ///   <para>
        ///     When a request comes in (from a browser) this method will
        ///     be called by the Networking code.  Each line of the HTTP request
        ///     will come as a separate message.  The "line" we are interested in
        ///     is a PUT or GET request.  
        ///   </para>
        ///   <para>
        ///     The following messages are actionable:
        ///   </para>
        ///   <para>
        ///      get highscore - respond with a highscore page
        ///   </para>
        ///   <para>
        ///      get favicon - don't do anything (we don't support this)
        ///   </para>
        ///   <para>
        ///      get scores/name - along with a name, respond with a list of scores for the particular user
        ///   <para>
        ///      get scores/name/highmass/highrank/startime/endtime - insert the appropriate data
        ///      into the database.
        ///   </para>
        ///   </para>
        ///   <para>
        ///     create - contact the DB and create the required tables and seed them with some dummy data
        ///   </para>
        ///   <para>
        ///     get index (or "", or "/") - send a happy home page back
        ///   </para>
        ///   <para>
        ///     get css/styles.css?v=1.0  - send your sites css file data back
        ///   </para>
        ///   <para>
        ///     otherwise send a page not found error
        ///   </para>
        ///   <para>
        ///     Warning: when you send a response, the web browser is going to expect the message to
        ///     be line by line (new line separated) but we use new line as a special character in our
        ///     networking object.  Thus, you have to send _every line of your response_ as a new Send message.
        ///   </para>
        /// </summary>
        /// <param name="network_message_state"> provided by the Networking code, contains socket and message</param>
        internal static void onMessage(Networking channel, string message)
        {
            Console.WriteLine(message);
            Console.WriteLine($"Message received: {message}");
            if (!clientRequestData.ContainsKey(channel))
            {
                clientRequestData[channel] = new StringBuilder();
            }
            clientRequestData[channel].Append(message);


            if (message.StartsWith("GET "))
            {
                var requestLine = message.Split(' ');
                var requestPath = requestLine[1].ToLower();

                if (requestPath == "/" || requestPath == "/index" || requestPath == "/index.html")
                {
                    string response = BuildMainPage();
                    string[] responseLines = response.Split("\n");

                    foreach (string line in responseLines)
                    {
                        channel.Send(line);
                    }
                }
                else if (requestPath.StartsWith("/css/"))
                {
                    string cssResponse = SendCSSResponse();
                    string[] cssResponseLines = cssResponse.Split("\n");

                    foreach (string line in cssResponseLines)
                    {
                        channel.Send(line);
                    }
                }
                else if (requestPath == "/createdbtables")
                {
                    string dbTablesPage = CreateDBTablesPage();
                    string[] dbTablesPageLines = dbTablesPage.Split('\n');

                    foreach (string line in dbTablesPageLines)
                    {
                        channel.Send(line);
                    }
                }
                else if (requestPath == "/highscore")
                {
                    string response = BuildHighscorePage();
                    string[] responseLines = response.Split("\n");

                    foreach (string line in responseLines)
                    {
                        channel.Send(line);
                    }
                    AlivePlayersRank(GameIDWorld);
                    UpdateAgarioPlayerDetailedData(GameIDWorld);

                }
                else if (requestPath.StartsWith("/scores/name"))
                {
                    string response = BuildUserScoresPage();
                    string[] responseLines = response.Split("\n");

                    foreach (string line in responseLines)
                    {
                        channel.Send(line);
                    }
                }
                else if (requestPath.StartsWith("/data"))
                {
                    // Extract the player's name from the URL
                    var uri = new Uri($"http://localhost{requestPath}");
                    var queryParameters = HttpUtility.ParseQueryString(uri.Query);
                    string playerName = queryParameters["playerName"];
                    long playerId = long.Parse(queryParameters["playerId"]);
                    float Mass = float.Parse(queryParameters["Mass"]);
                    string StartTime = queryParameters["StartTime"];
                    string DeadTime = queryParameters["DeadTime"];
                    lock (playerDataLock)
                    {
                        if (playerId != 0 && playerId != null)
                        {
                            PlayerData playerData = new PlayerData
                            {
                                PlayerName = playerName,
                                Mass = Mass,
                                StartTime = StartTime,
                                DeadTime = DeadTime
                            };

                            playerDataDictionary[playerId] = playerData;
                            AddDataToDatabase(GameIDWorld);
                        }
                    }
                }
                else if (requestPath.StartsWith("/scores/"))
                {
                    string playerName = requestPath.Substring(8);
                    string response = BuildSpecificPlayerScoresPage(playerName);
                    string[] responseLines = response.Split("\n");

                    foreach (string line in responseLines)
                    {
                        channel.Send(line);
                    }
                }
                else if (requestPath == "/fancy")
                {
                    string response = BuildFancyPage();
                    string[] responseLines = response.Split("\n");

                    foreach (string line in responseLines)
                    {
                        channel.Send(line);
                    }
                }
                else if (requestPath == "/create")
                {
                    string createResponse = CreateDBTablesPage();
                    string[] createResponseLines = createResponse.Split('\n');

                    foreach (string line in createResponseLines)
                    {
                        channel.Send(line);
                    }
                }


                else
                {
                    string notFoundResponse = "HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\n\r\n<h1>404 - Page Not Found</h1>";
                    string[] notFoundResponseLines = notFoundResponse.Split("\r\n");

                    foreach (string line in notFoundResponseLines)
                    {
                        channel.Send(line);
                    }
                }

                counter++;
                channel.Disconnect();
            }
        }

        /// <summary>
        /// Fetch the specific player data
        /// </summary>
        /// <param name="playerName"> the player name</param>
        /// <returns></returns>
        private static DataTable FetchSpecificPlayerScoresData(string playerName)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"SELECT Game_ID, Player_ID, Player_Name, HighestMass, GameTimeLastedInSeconds, StartTime, DeadTime
                             FROM AgarioPlayerDetailedData
                             WHERE Player_Name = @playerName
                             ORDER BY Game_ID ASC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@playerName", playerName);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }

            return dataTable;
        }

        /// <summary>
        /// Builds the get user scores page
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private static string BuildSpecificPlayerScoresPage(string playerName)
        {
            DataTable specificPlayerScoresData = FetchSpecificPlayerScoresData(playerName);
            StringBuilder specificPlayerScoresTable = new StringBuilder();
            string cssLink = "<link rel='stylesheet' href='/css/styles.css'>";

            specificPlayerScoresTable.Append("<table><thead><tr><th>Game ID</th><th>Player ID</th><th>Player Name</th><th>Mass</th><th>Game Time Lasted In Seconds</th><th>StartTime</th><th>DeadTime</th></tr></thead><tbody>");

            foreach (DataRow row in specificPlayerScoresData.Rows)
            {
                specificPlayerScoresTable.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>", row["Game_ID"], row["Player_ID"], row["Player_Name"], row["HighestMass"], row["GameTimeLastedInSeconds"], row["StartTime"], row["DeadTime"]);
            }

            specificPlayerScoresTable.Append("</tbody></table>");


            string message = $"<h1>{playerName}'s Scores Page</h1>{cssLink}{specificPlayerScoresTable}";
            string header = BuildHTTPResponseHeader(message.Length);


            return header + message;
        }

        /// <summary>
        /// Builds the fancy page
        /// </summary>
        /// <returns></returns>
        private static string BuildFancyPage()
        {
            DataTable dataTable = FetchHighscoreData();

            StringBuilder dataLabels = new StringBuilder("[");
            StringBuilder dataValues = new StringBuilder("[");
            foreach (DataRow row in dataTable.Rows)
            {
                dataLabels.AppendFormat("'{0}',", row["Player_Name"]);
                dataValues.AppendFormat("{0},", row["Mass"]);
            }
            dataLabels.Append("]");
            dataValues.Append("]");

            string htmlContent = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
        </head>
        <body>
            <h1>Fancy Highscore Chart</h1>
            <canvas id='myChart'></canvas>
            <script>
                var ctx = document.getElementById('myChart').getContext('2d');
                var myChart = new Chart(ctx, {{
                    type: 'bar',
                    data: {{
                        labels: {dataLabels},
                        datasets: [{{
                            label: 'Highscore by Player',
                            data: {dataValues},
                            backgroundColor: 'rgba(75, 192, 192, 0.2)',
                            borderColor: 'rgba(75, 192, 192, 1)',
                            borderWidth: 1
                        }}]
                    }},
                    options: {{
                        scales: {{
                            y: {{
                                beginAtZero: true
                            }}
                        }}
                    }}
                }});
            </script>
        </body>
        </html>";

            string header = BuildHTTPResponseHeader(htmlContent.Length, "text/html");

            return header + htmlContent;
        }

        /// <summary>
        /// Handle some CSS to make our pages beautiful
        /// </summary>
        /// <returns>HTTP Response Header with CSS file contents added</returns>
        private static string SendCSSResponse()
        {
            string cssContent;

            cssContent = @"/* styles.css */
body {
    font-family: Arial, sans-serif;
    font-size: 16px;
    line-height: 1.6;
    color: #333;
    background-color: yellow; /* Updated background color to yellow */
    margin: 0;
    padding: 0;
}

.centered-content {
    text-align: center;
    background-color: yellow;
    padding: 20px;
}


h1 {
    font-size: 2em;
    margin-bottom: 16px;
}

.btn {
    display: inline-block;
    background-color: #333;
    color: #fff;
    text-decoration: none;
    padding: 12px 24px; /* Increased padding for a larger button */
    border-radius: 4px;
    margin: 8px;
    font-size: 1.1em; /* Increased font size for better readability */
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2); /* Added a subtle box shadow */
    transition: background-color 0.3s, box-shadow 0.3s; /* Added a transition for smooth hover effects */
}

.btn:hover {
    background-color: #666; /* Change the background color on hover */
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.4); /* Increase the box shadow size on hover */
}


th, td {
    border: 1px solid black;
    padding: 8px;
    text-align: left;
}

th {
    background-color: #f2f2f2;
}

";

            string header = BuildHTTPResponseHeader(cssContent.Length, "text/css");
            return header + cssContent;
        }

        /// <summary>
        /// Record the instant data when a player becomes number one in the game
        /// </summary>
        /// <param name="GameIDData"></param>
        private static void AlivePlayersRank(Dictionary<int, Dictionary<long, PlayerData>> GameIDData)
        {
            Dictionary<long, PlayerData> CurrentGameData = GameIDData[CurrentGameID];
            lock (AddplayerDataLock)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        string query = @"SELECT Player_Name FROM AlivePlayersRank WHERE Player_ID = @PlayerID";
                        using SqlCommand command = new SqlCommand(query, con);
                        foreach (var item in CurrentGameData)
                        {
                            if (item.Key != 0)
                            {
                                command.Parameters.Clear();
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@PlayerID", item.Key);
                                string existingPlayerName = (string)command.ExecuteScalar();

                                if (existingPlayerName == null)
                                {
                                    query = @"INSERT INTO AlivePlayersRank(Player_ID, Player_Name, Game_ID, Mass, Alive) VALUES (@PlayerID, @playerName, @gameid, @mass, @alive)";
                                    command.CommandText = query;
                                    command.Parameters.AddWithValue("@playerName", item.Value.PlayerName);
                                    command.Parameters.AddWithValue("@gameid", CurrentGameID);
                                    command.Parameters.AddWithValue("@mass", item.Value.Mass);
                                    command.Parameters.AddWithValue("alive", item.Value.DeadTime.ToString().Equals(""));
                                    command.ExecuteNonQuery();
                                }
                                else if (existingPlayerName != null)
                                {
                                    query = @"UPDATE AlivePlayersRank SET Player_Name = @playerName, Mass = @mass, Alive = @alive WHERE Player_ID = @PlayerID";
                                    command.CommandText = query;
                                    command.Parameters.AddWithValue("@playerName", item.Value.PlayerName);
                                    command.Parameters.AddWithValue("@mass", item.Value.Mass);
                                    command.Parameters.AddWithValue("alive", item.Value.DeadTime.ToString().Equals(""));
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch (SqlException exception)
                {
                    Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
                }
            }
        }

        /// <summary>
        /// Record the instant data when a player becomes number one in the game
        /// </summary>
        /// <param name="GameIDData"></param>
        private static void NumberOnePlayers(Dictionary<int, Dictionary<long, PlayerData>> GameIDData)
        {
            Dictionary<long, PlayerData> CurrentGameData = GameIDData[CurrentGameID];
            long CurrentNumberOneID = 0;
            float CurrentNumberOneMass = 0;

            // Find the player with the highest mass
            foreach (var item in CurrentGameData)
            {
                if (CurrentNumberOneMass < item.Value.Mass)
                {
                    CurrentNumberOneMass = item.Value.Mass;
                    CurrentNumberOneID = item.Key;
                }
            }

            lock (AddplayerDataLock)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        string query = @"SELECT Player_Name FROM PlayerNumberOneData WHERE Player_ID = @PlayerID";
                        using SqlCommand command = new SqlCommand(query, con);
                        command.Parameters.AddWithValue("@PlayerID", CurrentNumberOneID);
                        string existingPlayerName = (string)command.ExecuteScalar();

                        if (existingPlayerName == null)
                        {
                            query = @"INSERT INTO PlayerNumberOneData(Player_ID, Player_Name, Game_ID, MassAtNumberOne, TimeBecameFirst) VALUES (@PlayerID, @playerName, @gameid, @mass, @time)";
                            command.CommandText = query;
                            command.Parameters.AddWithValue("@playerName", CurrentGameData[CurrentNumberOneID].PlayerName);
                            command.Parameters.AddWithValue("@gameid", CurrentGameID);
                            command.Parameters.AddWithValue("@mass", CurrentNumberOneMass);
                            string timestring = DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss");
                            if (TimeSpan.TryParseExact(timestring, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan timeValue))
                            {
                                command.Parameters.AddWithValue("@time", timeValue);
                            }
                            command.ExecuteNonQuery();
                        }
                        else
                        {
                            query = @"UPDATE PlayerNumberOneData SET Player_Name = @playerName, MassAtNumberOne = @mass, TimeBecameFirst = @time WHERE Player_ID = @PlayerID AND Game_ID = @gameid";
                            command.CommandText = query;
                            command.Parameters.AddWithValue("@playerName", CurrentGameData[CurrentNumberOneID].PlayerName);
                            command.Parameters.AddWithValue("@mass", CurrentNumberOneMass);
                            string timestring = DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss");
                            if (TimeSpan.TryParseExact(timestring, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan timeValue))
                            {
                                command.Parameters.AddWithValue("@time", timeValue);
                            }
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (SqlException exception)
                {
                    Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
                }
            }
        }

        /// <summary>
        /// Records the player's data with player's Highest Mass before the player dies and the game last.
        /// </summary>
        /// <param name="GameIDData"></param>
        private static void UpdateAgarioPlayerDetailedData(Dictionary<int, Dictionary<long, PlayerData>> GameIDData)
        {
            Dictionary<long, PlayerData> CurrentGameData = GameIDData[CurrentGameID];
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = @"SELECT Player_Name FROM AgarioPlayerDetailedData WHERE Player_ID = @PlayerID";
                    using SqlCommand command = new SqlCommand(query, con);
                    foreach (var item in CurrentGameData)
                    {
                        if (item.Key != 0)
                        {
                            command.Parameters.Clear();
                            command.CommandText = query;
                            command.Parameters.AddWithValue("@PlayerID", item.Key);
                            string existingPlayerName = (string)command.ExecuteScalar();

                            if (existingPlayerName == null)
                            {
                                query = @"INSERT INTO AgarioPlayerDetailedData(Player_ID, Player_Name, Game_ID, StartTime) VALUES (@PlayerID, @playerName, @gameid, @starttime)";
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@playerName", item.Value.PlayerName);
                                command.Parameters.AddWithValue("@gameid", CurrentGameID);

                                command.Parameters.AddWithValue("@starttime", item.Value.StartTime);


                                command.ExecuteNonQuery();
                            }
                            else if (existingPlayerName != null)
                            {
                                query = @"UPDATE AgarioPlayerDetailedData SET Player_Name = @playerName, StartTime = @starttime WHERE Player_ID = @PlayerID";
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@playerName", item.Value.PlayerName);
                                command.Parameters.AddWithValue("@starttime", item.Value.StartTime);

                                command.ExecuteNonQuery();
                            }
                            if (!item.Value.DeadTime.ToString().Equals("") && item.Value.UpdatedHighestMass is false)
                            {
                                query = "UPDATE AgarioPlayerDetailedData SET HighestMass = @highestMass , DeadTime = @deadtime, GameTimeLastedInSeconds = @second WHERE Player_ID = @PlayerID";
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@highestMass", item.Value.Mass);
                                command.Parameters.AddWithValue("@deadtime", item.Value.DeadTime);
                                int second = (int)(TimeSpan.Parse(item.Value.DeadTime) - TimeSpan.Parse(item.Value.StartTime)).TotalSeconds;
                                command.Parameters.AddWithValue("@second", second);
                                item.Value.UpdatedHighestMass = true;
                                command.ExecuteNonQuery();
                            }
                            if (item.Value.DeadTime.ToString().Equals("") && item.Value.UpdatedHighestMass is true)
                            {
                                query = "UPDATE AgarioPlayerDetailedData SET HighestMass = @highestMass, DeadTime = @deadtime, GameTimeLastedInSeconds = @second WHERE Player_ID = @PlayerID";
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@highestMass", null);
                                command.Parameters.AddWithValue("@deadtime", item.Value.DeadTime);
                                command.Parameters.AddWithValue("@second", null);
                                item.Value.UpdatedHighestMass = false;
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }
        }

        /// <summary>
        /// Add data to the databases : AgarioGame, AgarioPlayer
        /// </summary>
        /// <param name="GameIDData"></param>
        private static void AddDataToDatabase(Dictionary<int, Dictionary<long, PlayerData>> GameIDData)
        {
            Dictionary<long, PlayerData> CurrentGameData = GameIDData[CurrentGameID];
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT Game_ID FROM AgarioGame WHERE Game_ID = @gameid";
                    using SqlCommand checkGameIDCommand = new SqlCommand(query, con);
                    checkGameIDCommand.Parameters.AddWithValue("@gameid", CurrentGameID);
                    object gameIDResult = checkGameIDCommand.ExecuteScalar();
                    if (gameIDResult == null)
                    {
                        query = @"INSERT INTO AgarioGame(CountPlayers, Game_ID) VALUES (@CountPlayer, @gameid)";
                        using SqlCommand insertGameCommand = new SqlCommand(query, con);
                        insertGameCommand.Parameters.AddWithValue("@CountPlayer", CurrentGameData.Count);
                        insertGameCommand.Parameters.AddWithValue("@gameid", CurrentGameID);
                        insertGameCommand.ExecuteNonQuery();
                    }
                    else
                    {
                        query = @"UPDATE AgarioGame SET CountPlayers = @CountPlayer WHERE Game_ID = @gameid";
                        using SqlCommand updateGameCommand = new SqlCommand(query, con);
                        updateGameCommand.Parameters.AddWithValue("@CountPlayer", CurrentGameData.Count);
                        updateGameCommand.Parameters.AddWithValue("@gameid", CurrentGameID);
                        updateGameCommand.ExecuteNonQuery();
                    }

                    query = @"SELECT Player_Name FROM AgarioPlayer WHERE Player_ID = @PlayerID";
                    using SqlCommand command = new SqlCommand(query, con);
                    foreach (var item in CurrentGameData)
                    {
                        if (item.Key != 0)
                        {
                            command.Parameters.Clear();
                            command.CommandText = query;
                            command.Parameters.AddWithValue("@PlayerID", item.Key);
                            string existingPlayerName = (string)command.ExecuteScalar();

                            if (existingPlayerName == null)
                            {
                                query = @"INSERT INTO AgarioPlayer(Player_ID, Player_Name, Game_ID) VALUES (@PlayerID, @playerName, @gameid)";
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@playerName", item.Value.PlayerName);
                                command.Parameters.AddWithValue("@gameid", CurrentGameID);
                                command.ExecuteNonQuery();
                            }
                            else if (existingPlayerName != item.Value.PlayerName)
                            {
                                query = @"UPDATE AgarioPlayer SET Player_Name = @playerName WHERE Player_ID = @PlayerID";
                                command.CommandText = query;
                                command.Parameters.AddWithValue("@playerName", item.Value.PlayerName);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection:\n   - {exception.Message}");
            }
        }

        /// <summary>
        ///    (1) Instruct the DB to seed itself (build tables, add data)
        ///    (2) Report to the web browser on the success
        /// </summary>
        /// <returns> the HTTP response header followed by some informative information</returns>
        private static string CreateDBTablesPage()
        {
            StringBuilder response = new StringBuilder();

            string createTablesScript = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgarioGame')
BEGIN
    CREATE TABLE [dbo].[AgarioGame] (
        [Game_ID]      INT NOT NULL,
        [CountPlayers] INT NOT NULL,
        PRIMARY KEY CLUSTERED ([Game_ID] ASC)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgarioPlayer')
BEGIN
    CREATE TABLE [dbo].[AgarioPlayer] (
        [Player_ID]   BIGINT         NOT NULL,
        [Game_ID]     INT            NOT NULL,
        [Player_Name] NVARCHAR (100) NOT NULL,
        PRIMARY KEY CLUSTERED ([Game_ID] ASC, [Player_ID] ASC, [Player_Name] ASC),
        FOREIGN KEY ([Game_ID]) REFERENCES [dbo].[AgarioGame] ([Game_ID])
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AgarioPlayerDetailedData')
BEGIN
    CREATE TABLE [dbo].[AgarioPlayerDetailedData] (
        [Game_ID]                 INT            NOT NULL,
        [Player_ID]               BIGINT         NOT NULL,
        [Player_Name]             NVARCHAR (100) NOT NULL,
        [HighestMass]             FLOAT (53)     NULL,
        [GameTimeLastedInSeconds] INT            NULL,
        [StartTime]               VARCHAR (100)  NULL,
        [DeadTime]                VARCHAR (100)  NULL,
        FOREIGN KEY ([Game_ID], [Player_ID], [Player_Name]) REFERENCES [dbo].[AgarioPlayer] ([Game_ID], [Player_ID], [Player_Name])
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AlivePlayersRank')
BEGIN
    CREATE TABLE [dbo].[AlivePlayersRank] (
        [Game_ID]     INT            NOT NULL,
        [Player_ID]   BIGINT         NOT NULL,
        [Player_Name] NVARCHAR (100) NOT NULL,
        [Mass]        FLOAT (53)     NOT NULL,
        [Alive]       BIT            NOT NULL,
        FOREIGN KEY ([Game_ID], [Player_ID], [Player_Name]) REFERENCES [dbo].[AgarioPlayer] ([Game_ID], [Player_ID], [Player_Name])
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlayerNumberOneData')
BEGIN
    CREATE TABLE [dbo].[PlayerNumberOneData] (
        [Game_ID]         INT            NOT NULL,
        [Player_ID]       BIGINT         NOT NULL,
        [Player_Name]     NVARCHAR (100) NOT NULL,
        [MassAtNumberOne] FLOAT (53)     NULL,
        [TimeBecameFirst] VARCHAR (50)   NULL,
        PRIMARY KEY CLUSTERED ([Game_ID] ASC, [Player_ID] ASC)
    );
END
";


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(createTablesScript, connection);
                    int tablesCreatedCount = command.ExecuteNonQuery();

                    string createTrigger1Script = @"
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_AlivePlayersRank_Insert')
BEGIN
    CREATE TRIGGER trg_AlivePlayersRank_Insert
    ON dbo.AlivePlayersRank
    INSTEAD OF INSERT
    AS
    BEGIN
        IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.AgarioPlayer ap ON i.Game_ID = ap.Game_ID AND i.Player_ID = ap.Player_ID AND i.Player_Name = ap.Player_Name)
        BEGIN
            INSERT INTO dbo.AlivePlayersRank (Game_ID, Player_ID, Player_Name, Mass, Alive)
            SELECT Game_ID, Player_ID, Player_Name, Mass, Alive FROM inserted;
        END
        ELSE
        BEGIN
            RAISERROR('Invalid Game_ID, Player_ID, or Player_Name. Cannot insert into AlivePlayersRank.', 16, 1);
            ROLLBACK TRANSACTION;
        END
    END;
END";

                    command = new SqlCommand(createTrigger1Script, connection);
                    string createTrigger1ProcedureScript = @"
CREATE PROCEDURE Create_AlivePlayersRank_Insert_Trigger
AS
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_AlivePlayersRank_Insert')
BEGIN
    EXEC('CREATE TRIGGER trg_AlivePlayersRank_Insert
    ON dbo.AlivePlayersRank
    INSTEAD OF INSERT
    AS
    BEGIN
        IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.AgarioPlayer ap ON i.Game_ID = ap.Game_ID AND i.Player_ID = ap.Player_ID AND i.Player_Name = ap.Player_Name)
        BEGIN
            INSERT INTO dbo.AlivePlayersRank (Game_ID, Player_ID, Player_Name, Mass, Alive)
            SELECT Game_ID, Player_ID, Player_Name, Mass, Alive FROM inserted;
        END
        ELSE
        BEGIN
            RAISERROR(''Invalid Game_ID, Player_ID, or Player_Name. Cannot insert into AlivePlayersRank.'', 16, 1);
            ROLLBACK TRANSACTION;
        END
    END');
END
";

                    string createTrigger2ProcedureScript = @"
CREATE PROCEDURE Create_PlayerNumberOneData_Insert_Trigger
AS
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_PlayerNumberOneData_Insert')
BEGIN
    EXEC('CREATE TRIGGER trg_PlayerNumberOneData_Insert
    ON dbo.PlayerNumberOneData
    INSTEAD OF INSERT
    AS
    BEGIN
        IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.AgarioPlayer ap ON i.Game_ID = ap.Game_ID AND i.Player_ID = ap.Player_ID AND i.Player_Name = ap.Player_Name)
        BEGIN
            INSERT INTO dbo.PlayerNumberOneData (Game_ID, Player_ID, Player_Name, MassAtNumberOne, TimeBecameFirst)
            SELECT Game_ID, Player_ID, Player_Name, MassAtNumberOne, TimeBecameFirst FROM inserted;
        END
        ELSE
        BEGIN
            RAISERROR(''Invalid Game_ID, Player_ID, or Player_Name. Cannot insert into PlayerNumberOneData.'', 16, 1);
            ROLLBACK TRANSACTION;
        END
    END');
END
";
                    // Create stored procedures for triggers
                    command = new SqlCommand(createTrigger1ProcedureScript, connection);
                    int trigger1ProcedureCreatedCount = command.ExecuteNonQuery();

                    command = new SqlCommand(createTrigger2ProcedureScript, connection);
                    int trigger2ProcedureCreatedCount = command.ExecuteNonQuery();

                    // Execute stored procedures to create triggers
                    command = new SqlCommand("EXEC Create_AlivePlayersRank_Insert_Trigger", connection);
                    int trigger1CreatedCount = command.ExecuteNonQuery();

                    command = new SqlCommand("EXEC Create_PlayerNumberOneData_Insert_Trigger", connection);
                    int trigger2CreatedCount = command.ExecuteNonQuery();


                    connection.Close();

                    // Building the HTTP response header
                    response.AppendLine("HTTP/1.1 200 OK");
                    response.AppendLine("Content-Type: text/html");
                    response.AppendLine();

                    // Adding informative information
                    response.AppendLine("<!DOCTYPE html>");
                    response.AppendLine("<html>");
                    response.AppendLine("<head>");
                    response.AppendLine("<title>Database Tables Created</title>");
                    response.AppendLine("</head>");
                    response.AppendLine("<body>");

                    if (tablesCreatedCount > 0)
                    {
                        response.AppendLine("<h1>Database Tables Created Successfully</h1>");
                        response.AppendLine("<p>The AgarioGame, AgarioPlayer, AgarioPlayerDetailedData, AlivePlayersRank, and PlayerNumberOneData tables have been created successfully.</p>");
                    }
                    else
                    {
                        response.AppendLine("<h1>Database Tables Already Exist</h1>");
                        response.AppendLine("<p>The AgarioGame, AgarioPlayer, AgarioPlayerDetailedData, AlivePlayersRank, and PlayerNumberOneData tables already exist.</p>");
                    }

                    response.AppendLine("</body>");
                    response.AppendLine("</html>");
                }
            }
            catch (Exception ex)
            {
                // Building the HTTP response header
                response.AppendLine("HTTP/1.1 500 Internal Server Error");
                response.AppendLine("Content-Type: text/html");
                response.AppendLine();

                // Adding informative information
                response.AppendLine("<!DOCTYPE html>");
                response.AppendLine("<html>");
                response.AppendLine("<head>");
                response.AppendLine("<title>Error Creating Database Tables</title>");
                response.AppendLine("</head>");
                response.AppendLine("<body>");
                response.AppendLine("<h1>Error Creating Database Tables</h1>");
                response.AppendLine($"<p>An error occurred while creating the database tables: {ex.Message}</p>");
                response.AppendLine("</body>");
                response.AppendLine("</html>");
            }

            return response.ToString();
        }

        /// <summary>
        /// method to handle when the server disconnect
        /// </summary>
        /// <param name="channel"></param>
        internal static void OnDisconnect(Networking channel)
        {
            Debug.WriteLine($"Goodbye {channel.RemoteAddressPort}");
            clientRequestData.Remove(channel);
        }

        /// <summary>
        /// This class stores the player data
        /// </summary>
        public class PlayerData
        {
            public string PlayerName { get; set; }
            public float Mass { get; set; }
            public string StartTime { get; set; }
            public string DeadTime { get; set; }
            public bool UpdatedHighestMass { get; set; } = false;
        }
    }

}

