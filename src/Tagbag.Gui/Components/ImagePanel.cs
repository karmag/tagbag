using System.Windows.Forms;
using Tagbag.Core;

namespace Tagbag.Gui.Components;

public class ImagePanel : Panel
{
    private EntryCollection _EntryCollection;

    public ImageGrid ImageGrid;
    private ImageView ImageView;
    private bool _GridMode = false;

    public ImagePanel(EventHub eventHub,
                      EntryCollection entryCollection,
                      ImageCache imageCache)
    {
        _EntryCollection = entryCollection;

        ImageGrid = new ImageGrid(eventHub, entryCollection, imageCache);
        ImageGrid.Name = "ImageGrid";
        ImageGrid.Dock = DockStyle.Fill;

        ImageView = new ImageView(eventHub, entryCollection, imageCache);
        ImageView.Name = "ImageView";
        ImageView.Dock = DockStyle.Fill;

        ShowGrid(true);
        PreviewKeyDown += (_, ev) => { ev.IsInputKey = true; };
    }

    public void ShowGrid(bool grid)
    {
        _GridMode = grid;
        ImageGrid.SetActive(grid);
        ImageView.SetActive(!grid);
        Controls.Clear();

        if (grid)
            Controls.Add(ImageGrid);
        else
            Controls.Add(ImageView);
    }

    // Move the cursor up or down by the given amount of rows. If the
    // panel is in single mode the offset is instead applied to the
    // cursor index.
    public void MoveCursorRow(int offset)
    {
        if (_GridMode)
            ImageGrid.MoveCursor(0, offset);
        else
            _EntryCollection.MoveCursor(offset);
    }

    // Move the cursor up or down by the given amount of pages. If the
    // panel is in single mode the offset is instead applied to the
    // cursor index.
    public void MoveCursorPage(int offset)
    {
        if (_GridMode)
            ImageGrid.MovePage(offset);
        else
            _EntryCollection.MoveCursor(offset);
    }

    // Toggles marked status of entries based on grid position. If the
    // panel is in single mode and x and y are zero the current entry
    // will be toggled. Otherwise this command has no effect in single
    // mode.
    public void ToggleMarked(int x, int y)
    {
        if (_GridMode &&
            ImageGrid.GetEntryAt(x, y) is Entry entry)
        {
            _EntryCollection.SetMarked(entry.Id,
                                       !_EntryCollection.IsMarked(entry.Id));
        }
        else if (x == 0 && y == 0 &&
                 _EntryCollection.GetEntryAtCursor() is Entry entry2)
        {
            _EntryCollection.SetMarked(entry2.Id,
                                       !_EntryCollection.IsMarked(entry2.Id));
        }
    }
}
