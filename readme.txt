Tagbag

    Tagbag connects keywords with images.

        - Tags are keywords with optional values.
        - Images are not manipulated.
        - Tags are stored in the nearest .tagbag file.

    New tagbag

        To setup a new tagbag file:

            1. Use "New..." to select a new tagbag root.
            2. Switch to "Scan / Problems" mode.
            3. Run "Scan" and then "Fix" to import images.
            4. Save and start tagging.

    Common controls

        Enter       - Goto / execute command line.
        Escape      - Pop last filter.
        Control + S - Save.

        Tab             - Switch Grid / Single image display.
        Control + Enter - Switch Command / Browse mode.
        Control + T     - Switch Tags / Tag-summary mode.

        Control + Q        - Clear marks.
        Control + A        - Mark all visible images.
        Space              - Mark image.
        Shift + Arrow keys - Mark image and move.

        Controls change depending on mode, see full listing below.

    Command line

        Tag examples:

            tag                 - Add tag.
            tag value           - Add tag with value.
            tag "another value" - Add value with whitespace.
            tag 15              - Add tag that is number.
            -tag                - Remove tag and any related values.

        Filter examples:

            :tag       - Find images that have tag.
            :tag value - Find images where tag = value.
            :tag < 15  - Find images where tag value is < 15.

        See full tagging and filtering syntax below.

    Status bar

        38% --- 279 / 349 --- 60 marked --- [level = 5]
        [a]        [b]           [c]            [d]

            a - Position within the currently visible images. Shows
            "Top", "Bot", or a percentage.

            b - Visible-images / all-images.

            c - Number of marked images.

            d - Current filters.

Specifics

    .tagbag

        The .tagbag file contains all the information about
        tags and images. It's indicating a root position for
        the tagbag and all sub-directories are considered
        when scanning for images to include.

    Browse / command

        Browse mode is focused on navigating entries. Command
        mode is focused on manipulating tags. Both modes
        support most functions, the main difference is in the
        amount of modifier keys or key strokes required to
        perform an action.

        Swap between browse and command mode with [Control +
        Enter].

    Marks

        Marking allows you to apply tag commands to multiple
        entries at once. When one or more entries are marked
        all tag commands are applied to all those entries.
        Marks persist until removed and are not affected by
        filters.

        Entries can be marked with [Space] and [Shift + Arrow
        Key]. See the mark/* and mark-and-move/* actions for
        further options.

        Use [Control + Q] to clear all marks. The number of
        marks are displayed in the status bar.

    Scan / Problems

        Scanning is used to add images and to find a number
        of problems. When images are added they are populated
        with width, height, filesize, and hash/sha256 tags.
        These tags are used to identify the image in problem
        fixing and duplication detection.

            File moved - A file has been moved within the
            tagbag sub-directories. The corresponding entry
            is updated to point at the new location.

            File missing - A file that is deleted or moved
            out of the tagbag sub-directory. The
            corresponding entry is deleted.

            Duplicate files - Two, or more, files are binary
            equivalent. All but one of them are removed and
            the tags from all entries are merged into the
            remaining entry.

    Duplicate detection

        Finds images with similar color profile. This can
        find images that have been resized, flipped, rotated,
        and cropped. Duplicates are found and removed in a
        three step process.

            Populate hashes - This step adds a hash/color tag
            to each image for use in comparisons. Only images
            that lacks the tag are populated.

            Find matches - Uses the color information to find
            similar images. Images who's similarity is
            smaller than the given threshold are tagged with
            the tag "duplicate" and a number. At this point
            you should verify the findings and remove false
            positives manually.

                Use   :duplicate
                and   !sort-int duplicate
                in the command line to display matches.

            Delete duplicates - For each group of duplicates
            (entries with the same value in their duplicate
            tag are a group) deletes all but one of them.
            Deleted files are moved to the recycle bin. Tags
            in deleted entries are merged into the remaining
            entry. The surviving file is chosen based on
            image dimensions, filesize, directory depth, and
            path length - in that order.

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
 +---------------------+-------------+
 | Action              | Key         |
 +---------------------+-------------+
 | backup              | Control + B |
 | mode/grid           | F1          |
 | mode/options        | F4          |
 | mode/scan           | F2          |
 | mode/scan-duplicate | F3          |
 | save                | Control + S |
 +---------------------+-------------+

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
