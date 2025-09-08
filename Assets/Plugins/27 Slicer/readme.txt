27 Slicer
Copyright 2021 Deftly

A detailed manual and API reference can be found online at https://slicer.deftly.games/

Have a question, suggestion or comment? Let us know on the Unity Forum. https://forum.unity.com/threads/1200301/

Have an issue or found a bug? Email the details through to info@deftly.games

INITIAL SETUP AND UPGRADING

These steps should also be followed when both upgrading versions and installing for the first time.

    Backup your project.
    This process is not likely to cause an issue, but it is always better to be safe. If you are not already, have a look at using source code management software like Git.

    Upgrade Unity to 2019.4 or newer.
    27 Slicer is built with Unity 2019.4 and tested on select newer versions.

    Delete any older versions of 27 Slicer.
    Unfortunately Unity's package importer does not handle deleted or renamed files. This may lead to an compilation errors if the update has deleted or renamed any files.
    The default location of the folder is /Assets/Plugins/27 Slicer/.

    Import the 27 Slicer Unity Package.
    From the Asset Store:
        In Unity navigate to Window -> Package Manager.
        From the Package Manager, in the top left drop down select "Packages: My Assets".
        Search for "27 Slicer" and select the package to install and click import.
        Check all the files you wish to import (Feel free to uncheck the "Demos" Folder).
        Click Import.

    From a .unitypackage file:
        In Unity navigate to Assets -> Import Package -> Custom Package.
        Navigate to the .unitypackage file.
        Check all the files you wish to import (Feel free to uncheck the "Demos" Folder).
        Click Import.
