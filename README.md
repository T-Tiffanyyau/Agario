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

# Overview of the Agario

## Notes to the Grader
We are sorry to have the Agario client GUI in this repo. 

After we almost finished the project, we realized that we should not hand in the instrumented client.

We tried to seprate the program afterwards but we didnt succeed.

Although our submittion for the asignment at https://github.com/uofu-cs3500-spring23/assignment-nine---web-server---sql-dadada

has both gui & webserver, it is ready for grade.

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

# Partnership Information

Our code was mostly completed via pair programming such as making the webpage to be able to insert data to sql, making html pages, create sql table by html.
We also did some individual work such as writing readme.

# Branching

We did not use branching in this project. We did the project together in person and we feel like there is not a need to use branching.

commit numbers: 35

# Testing

We did not test the game with unit testing. But we did test the program and looking up localhost:11001 to check on the database multiple times.

# Time Expenditures:
```
Hours Estimated/Worked         Assignment                                           Note
          20  /  20            Assignment Nine - Web Server and SQL Integration     Everything on time
```

Editing Client GUI to be able to pass data to webserver: 6 hours
inserting data to sql & creating html pages: 9 hours
testing and debugging: 4 hours
writing readme: 1 hours
