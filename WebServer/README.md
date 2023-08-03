```
Author:     Man Wai Lam & Tiffany Yau
Date:       26 Apr 2023
Course:     CS 3500, University of Utah, School of Computing
GitHub ID:  matthewlam721 & T-Tiffanyyau
Repo:       https://github.com/uofu-cs3500-spring23/assignment-nine---web-server---sql-dadada
Solution:   Agario
Project:    WebServer
Copyright:  CS 3500, Tiffany Yau and Man Wai Lam - This work may not be copied for use in Academic Coursework.
```

# WebServer:

The WebServer class stores the player data to SQL and allows users to access it through localhost:11001.

It also builds the webpage on localhost:11001, where users can see the player data.

## Database Table Summary

    1. AgarioGame: Store the game id and number of players in the game. The game id is the primary key.

    2. AgarioPlayer: Store the player id, game id, and player name, creating primary data for the player object. The game id is the foreign key referencing from AgarioGame.

    3. AgarioPlayerDetailedData: Stores more information about the player, such as the mass, game lasted, start time, and dead time.
    It references the AgarioPlayer table but stores more customized information about the player.

    4. PlayerNumberOneData: The table is based on the AgarioPlayer. It stores data specifically about "At what time into the game was the player number one in size" and the mass at that time.

    5. AlivePlayersRank: The table is based on the AgarioPlayer. It stores player data and indicates whether the player is alive.
    So we could create a rank base on live players.


# References:

1. Assignment 9 - Web Server and SQL Integration: https://docs.google.com/document/d/1F_OPUo9qbNIKWLDcwsQzP2z4O8GWfQx4cHSxGxGTmGE/edit
