Tagbag

    Tagbag is an application for tagging images.

        - Tagging is manual.
        - The images are not manipulated.
        - Tags are stored in the nearest .tagbag file.
        - Primarily keyboard oriented.

Tags

    To manipulate tags enter commands in the input field.
    Tags consist of a key and any number of values, including
    none at all.

    Use [Enter] to access the input field and to execute
    commands. To see a summary of tags press [Ctrl + T].

Filters

    Filters limit the amount of entries shown. To add a
    filter prefix it in the input field with a colon. Filters
    goes on a stack and can be popped one at a time. Current
    filters are shown in the status bar.

    Use [Escape] to pop the current filter. The number of
    visible entries are shown in the status bar.

Marks

    Marking allows you to apply tag commands to multiple
    entries at once. When one or more entries are marked all
    tag commands are applied to all those entries. Marks
    persist until removed and are not affected by filters.

    Entries can be marked with [Space] and [Shift + Arrow
    Key]. See the mark/* and mark-and-move/* actions for
    further options.

    Use [Ctrl + Q] to clear all marks. The number of marks
    are displayed in the status bar.

Browse / command

    Browse mode is focused on navigating entries. Command
    mode is focused on manipulating tags. Both modes support
    most functions, the main difference is in the amount of
    modifier keys or key strokes required to perform an
    action.

    Swap between browse and command mode with [Ctrl + Enter].

Grid / single

    Grid mode show multiple entries. Single mode shows one
    entry.

    Swap between grid and single with [Tab].

Scan

    Scanning is used to add images to the tagbag as well as
    fix problems. Access the Scan page with [F2] or the menu.
    Use [F1] to return to grid view.

Save

    Modifications to entries needs to be saved manually. Use
    [Ctrl + S] or the menu to save.

----------------------------------------------------------------------

Tagging

    Tag grammar
        tag-command = unary | binary | ternary
        unary       = ['+' | '-'] tag
        binary      = tag value
        ternary     = tag op value
        tag         = symbol
        op          = '+' | '-' | '='
        value       = int | string | symbol

        Operators
            + : Add the value to tag.
            - : Remove the value from tag.
            = : Set the tag to have value, removing other values.

    Examples
        river
            Add the tag 'river' to the entry.
        score 10
            Add the value 10 to the tag 'score'.
        author = 'Quill Penhammer'
            Set the 'author' tag to be 'Quill Penhammer' overwriting
            any old values.
        -normal
            Remove the tag normal.

----------------------------------------------------------------------

Filtering

    Filter grammar
        filter-command = unary | binary | ternary | negated
        unary          = tag
        binary         = tag value
        ternary        = tag op value
        negated        = 'not' (unary | binary | ternary)
        tag            = symbol
        op             = '=' | '~=' | '<' | '<=' | '>' | '>='
        value          = int | string | symbol

        A string is characters enclosed in doublequotes. A symbol is a
        letter followed by other non-whitespace characters. A symbol
        will be interpreted as a string in relevant contexts.

        Operators
            =            : equality
            ~=           : regular expression, ignores case
            <, <=, >, >= : general math operators

    Examples
        cloud
            Find any entry with the tag 'cloud' regardless of values.
        score 4
            Find any entry where the tag 'score' is equal to 4.
        year > 2000
            Find any entry where the tag 'year' is greater than 2000.
        not good
            Find any entry that doesn't have the tag 'good'.

----------------------------------------------------------------------

Common keys
 +-----------+-------------+
 | Action    | Key         |
 +-----------+-------------+
 | backup    | Control + B |
 | mode/grid | F1          |
 | mode/scan | F2          |
 | save      | Control + S |
 +-----------+-------------+

Grid keys
 +-------------------------+---------------------+---------------------+
 | Action                  | Browse              | Command             |
 +-------------------------+---------------------+---------------------+
 | copy-image-to-clipboard | Control + C         | Control + C         |
 | copy-path-to-clipboard  | Shift + Control + C | Shift + Control + C |
 | cursor/down             | Down                | Alt + Down          |
 |                         | Alt + Down          |                     |
 | cursor/left             | Control + K         | Control + K         |
 |                         | Left                | Alt + Left          |
 |                         | Alt + Left          |                     |
 | cursor/right            | Control + J         | Control + J         |
 |                         | Right               | Alt + Right         |
 |                         | Alt + Right         |                     |
 | cursor/up               | Up                  | Alt + Up            |
 |                         | Alt + Up            |                     |
 | entry/delete            | Control + D         | Control + D         |
 | filter/pop              | Escape              | Escape              |
 | mark-and-move/down      | Shift + Down        | Shift + Alt + Down  |
 |                         | Shift + Alt + Down  |                     |
 | mark-and-move/left      | Shift + Left        | Shift + Alt + Left  |
 |                         | Shift + Alt + Left  |                     |
 | mark-and-move/right     | Shift + Right       | Shift + Alt + Right |
 |                         | Shift + Alt + Right |                     |
 |                         | Space               |                     |
 | mark-and-move/up        | Shift + Up          | Shift + Alt + Up    |
 |                         | Shift + Alt + Up    |                     |
 | mark/all-visible        | Control + A         | Control + A         |
 | mark/clear              | Control + Q         | Control + Q         |
 | mark/grid-0-0           | Alt + Q             | Alt + Q             |
 | mark/grid-0-1           | Alt + A             | Alt + A             |
 | mark/grid-0-2           | Alt + Z             | Alt + Z             |
 | mark/grid-1-0           | Alt + W             | Alt + W             |
 | mark/grid-1-1           | Alt + S             | Alt + S             |
 | mark/grid-1-2           | Alt + X             | Alt + X             |
 | mark/grid-2-0           | Alt + E             | Alt + E             |
 | mark/grid-2-1           | Alt + D             | Alt + D             |
 | mark/grid-2-2           | Alt + C             | Alt + C             |
 | mark/grid-3-0           | Alt + R             | Alt + R             |
 | mark/grid-3-1           | Alt + F             | Alt + F             |
 | mark/grid-3-2           | Alt + V             | Alt + V             |
 | mark/grid-4-0           | Alt + T             | Alt + T             |
 | mark/grid-4-1           | Alt + G             | Alt + G             |
 | mark/grid-4-2           | Alt + B             | Alt + B             |
 | mode/browse             |                     | Control + Enter     |
 | mode/command            | Control + Enter     |                     |
 | mode/single             | Tab                 | Tab                 |
 | press-enter             | Enter               | Enter               |
 | refresh                 | Control + R         | Control + R         |
 | scroll/bottom           | End                 | Alt + End           |
 |                         | Alt + End           |                     |
 | scroll/page-down        | Next                | Next                |
 |                         | Alt + Next          | Alt + Next          |
 | scroll/page-up          | PageUp              | PageUp              |
 |                         | Alt + PageUp        | Alt + PageUp        |
 | scroll/top              | Home                | Alt + Home          |
 |                         | Alt + Home          |                     |
 | swap-tag-view           | Control + T         | Control + T         |
 +-------------------------+---------------------+---------------------+

Single keys
 +-------------------------+---------------------+---------------------+
 | Action                  | Browse              | Command             |
 +-------------------------+---------------------+---------------------+
 | copy-image-to-clipboard | Control + C         | Control + C         |
 | copy-path-to-clipboard  | Shift + Control + C | Shift + Control + C |
 | cursor/down             | Down                | Alt + Down          |
 |                         | Alt + Down          |                     |
 | cursor/left             | Control + K         | Control + K         |
 |                         | Left                | Alt + Left          |
 |                         | Alt + Left          |                     |
 | cursor/right            | Control + J         | Control + J         |
 |                         | Right               | Alt + Right         |
 |                         | Alt + Right         |                     |
 | cursor/up               | Up                  | Alt + Up            |
 |                         | Alt + Up            |                     |
 | entry/delete            | Control + D         | Control + D         |
 | filter/pop              | Escape              | Escape              |
 | mark-and-move/down      | Shift + Down        | Shift + Alt + Down  |
 |                         | Shift + Alt + Down  |                     |
 | mark-and-move/left      | Shift + Left        | Shift + Alt + Left  |
 |                         | Shift + Alt + Left  |                     |
 | mark-and-move/right     | Shift + Right       | Shift + Alt + Right |
 |                         | Shift + Alt + Right |                     |
 |                         | Space               |                     |
 | mark-and-move/up        | Shift + Up          | Shift + Alt + Up    |
 |                         | Shift + Alt + Up    |                     |
 | mark/all-visible        | Control + A         | Control + A         |
 | mark/clear              | Control + Q         | Control + Q         |
 | mode/browse             |                     | Control + Enter     |
 | mode/command            | Control + Enter     |                     |
 | mode/grid               | Tab                 | Tab                 |
 | press-enter             | Enter               | Enter               |
 | refresh                 | Control + R         | Control + R         |
 | scroll/bottom           | End                 | Alt + End           |
 |                         | Alt + End           |                     |
 | scroll/page-down        | Next                | Next                |
 |                         | Alt + Next          | Alt + Next          |
 | scroll/page-up          | PageUp              | PageUp              |
 |                         | Alt + PageUp        | Alt + PageUp        |
 | scroll/top              | Home                | Alt + Home          |
 |                         | Alt + Home          |                     |
 | swap-tag-view           | Control + T         | Control + T         |
 +-------------------------+---------------------+---------------------+

----------------------------------------------------------------------

Build commands
    Run              : dotnet run <optional-initial-path>
    Build executable : dotnet publish
    Test             : dotnet test
    Generate readme  : dotnet run --gen-doc

Standalone executable is located at
    bin\Release\net9.0-windows\win-x64\publish\tagbag.exe
