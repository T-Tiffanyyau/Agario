```
Author:     Man Wai Lam & Tiffany Yau
Date:       26 Apr 2023
Course:     CS 3500, University of Utah, School of Computing
GitHub ID:  matthewlam721 & T-Tiffanyyau
Repo:       https://github.com/uofu-cs3500-spring23/assignment-nine---web-server---sql-dadada
Solution:   Agario
Project:    Client GUI
Copyright:  CS 3500, Tiffany Yau and Man Wai Lam - This work may not be copied for use in Academic Coursework.
```

# Comments to ClientGUI:
The class is modified from assignment 8 to be able to send data to sql.

# Assignment Specific Topics:
Features
-Real-time rendering of the game world with zoom functionality
-Display of player names above their game characters
-Efficient use of color caching for improved performance
-Update loop running on a separate thread for smooth gameplay

#Usage
To initialize the GameWorldDrawable class, pass the initial World object, a Networking object for communication with the server, 
and a GraphicsView object for rendering the game world.

World Update
To update the game world with the latest data from the server, call the UpdateWorld method with a new World object.
Drawing the Game World

Customization
You can adjust the following parameters:
Zoom level: Modify ZoomX and ZoomY constants in the GameWorldDrawable class.
Player name font size and color: Adjust fontSize and fontColor variables in the DrawPlayerName and DrawCurrentPlayerName methods.

# Consulted Peers:

None

# References:

1. Assignment 9 - Web Server and SQL Integration: https://docs.google.com/document/d/1F_OPUo9qbNIKWLDcwsQzP2z4O8GWfQx4cHSxGxGTmGE/edit