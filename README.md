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

It is also an exploration of game development, tackling real-time networking, database operations, GUI rendering, game physics, mechanics, and server management.

It was written by C# and leverages a networking library for managing client-server communication and SQL for database operations.

## Classes in the project

- Agario Models: This is where the core game logic resides. It models the game world of Agario, including the objects within the game, such as the Players and Food, and how they interact.

- ClientGUI: This component handles the visual presentation of the game world on the client's end. It utilizes an update loop on a separate thread to ensure a smooth gameplay experience. Customization options are provided, like adjusting the zoom level and changing the font size and color for player names.

- FileLogger: This is my logging utility. It's in charge of creating a log file that stores log messages following a specific format. The format includes the timestamp, log level, and log message. This class proves invaluable for debugging, performance monitoring, or even usage analytics.

- WebServer: This is the nerve center of all server-side operations. It manages to store player data into an SQL database, providing users access to this data through a web server set up to run on localhost. It also generates webpages for users to interact with, offering data like high scores. This class handles HTTP requests and responses, crafting dynamic webpages based on the game's current state.
