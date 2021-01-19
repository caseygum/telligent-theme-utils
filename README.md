# telligent-theme-utils
.NetCore command-line app to explode/package exported theme files

**Extract Theme**

Given an exported Telligent theme file (or a file with multiple themes), expands out contents into a
directory structure.  This makes it easier to diff changes between theme versions.
````
ThemeUtils --extract --themeFile=PATH_TO_INPUT_THEME_FILE --outputDir=PATH_TO_OUTPUT_DIR
````

**Package Theme**

*Note: this isn't implemented yet*
````
ThemeUtils --pacakge --sourceDir=PATH_TO_SOURCE_DIR --themeFile=PATH_TO_OUTPUT_THEME_FILE 
````
