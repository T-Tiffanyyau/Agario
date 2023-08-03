```
Author:     Man Wai Lam & Tiffany Yau
Date:       26-April-2023
Course:     CS 3500, University of Utah, School of Computing
GitHub ID:  matthewlam721 & T-Tiffanyyau
Repo:       https://github.com/uofu-cs3500-spring23/assignment-nine---web-server---sql-dadada
Date:       (26-Apr-2023 9:05pm for Assignment 8)  (of when submission is ready to be evaluated)
Solution:   Agario
Copyright:  CS 3500, Tiffany Yau and Man Wai Lam - This work may not be copied for use in Academic Coursework.
```

# Agario

The Agario project is a comprehensive game development undertaking involving various client- and server-side operations aspects.

For me, it is also an exploration of game development, tackling real-time networking, database operations, GUI rendering, game physics, mechanics, and server management.

It was written by C# and leverages a networking library for managing client-server communication and SQL for database operations.

## Classes in the project

- Agario Models: This is where the core game logic resides. It models the game world of Agario, including the objects within the game, such as the Players and Food, and how they interact.

- ClientGUI: This component handles the visual presentation of the game world on the client's end. It utilizes an update loop on a separate thread to ensure a smooth gameplay experience. I've also provided some customization options, like adjusting the zoom level and changing the font size and color for player names.

- FileLogger: This is my logging utility. It's in charge of creating a log file that stores log messages following a specific format. The format includes the timestamp, log level, and the actual log message. This class proves invaluable for debugging, performance monitoring, or even usage analytics.

- WebServer: This is the nerve center of all server-side operations. It manages to store player data into an SQL database, providing users access to this data through a web server I set up to run on localhost. It also generates webpages for users to interact with, offering data like high scores. This class takes care of HTTP requests and responses, crafting dynamic webpages based on the game's current state.



# Database Table Summary
1. Briefly describe your DB architecture, i.e, what tables did you create and how are they related?   

    1. AgarioGame: Store the game id and number of players in the game. The game id is the primary key.

    2. AgarioPlayer: Store the player id, game id, and player name, creates a primary data for player object. The game id is the foreign key referencing from AgarioGame.

    3. AgarioPlayerDetailedData: Stores more information about the player, such as the mass, game lasted, start time and dead time.
    It references the AgarioPlayer table but just stores more customized information about the player.

    4. PlayerNumberOneData: The table is base on the AgarioPlayer. It stores data specifically about "At what time into the game was the player number one in size" and the mass at that time.

    5. AlivePlayersRank: The table is base on the AgarioPlayer. It stores player data and also indicate whether the player is alive or not.
    So we could create a rank base on alive players.

2. What (if any) non-standard pieces of data did you decide to put in your DB?

    I think the data we put in the database is pretty standard. We just store the player data we have in the program like start time, mass, name, etc.
