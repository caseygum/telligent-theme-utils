# telligent-theme-utils
.NetCore command-line app to explode/package exported theme files

**Extract Theme**

Given an exported Telligent theme file (or a file with multiple themes), expands out contents into a
directory structure.  This makes it easier to diff changes between theme versions.
````
ThemeUtils --extract [--clean] --themeFile=PATH_TO_INPUT_THEME_FILE --outputDir=PATH_TO_OUTPUT_DIR
````

**Package Theme**
Given an extracted Telligent theme directory, repackages contents into a theme XML file.
````
ThemeUtils --package [--clean] --sourceDir=PATH_TO_SOURCE_DIR --themeFile=PATH_TO_OUTPUT_THEME_FILE 
````
