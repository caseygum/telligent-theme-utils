# telligent-theme-utils
.NetCore command-line app to explode/package exported theme files

**Extract Theme**

Given an exported Telligent theme file (or a file with multiple themes), or exported widgets file, expands out contents into a
directory structure.  This makes it easier to diff changes between theme versions.
````
ThemeUtils --extract [--clean] --themeFile=PATH_TO_INPUT_THEME_OR_WIDGET_FILE --outputDir=PATH_TO_OUTPUT_DIR
````

**Package Theme**
Given an extracted Telligent theme or widgets directory, repackages contents into a theme XML file.
````
ThemeUtils --package [--clean] --sourceDir=PATH_TO_SOURCE_DIR --themeFile=PATH_TO_OUTPUT_THEME_OR_WIDGET_FILE 
````
