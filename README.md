# HangFixer

> Detects Visual Studio hangs while opening solutions and attempts to fix them


In certain circumstances that are hard to determine, Visual Studio may hang when loading a solution. 

This is typically solved by manually deleting the hidden .vs directory, or the .suo file, or both.

This extension does that automatically for you by:

1. Writing a temporary flag named after the solution file, with a .tmp extension
2. Deleting it after a successfull solution load.


If the solution didn't load successfully, you will likely kill the process and try again. At this point, step 2 above wouldn't have run, and the flag file will be present. We consider this the signal that something failed on the previous load, and procceed to delete the .vs directory and .suo files we find, before letting Visual Studio proceed with the solution load.

The logic is [pretty straightforward](https://github.com/MobileEssentials/HangFixer/blob/master/HangFixer/HangFixerPackage.cs#L35) and effective.