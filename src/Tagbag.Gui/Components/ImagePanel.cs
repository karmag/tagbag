using System.Windows.Forms;

namespace Tagbag.Gui.Components;

public class ImagePanel : Panel
{
    public ImageGrid ImageGrid;
    private ImageView ImageView;
    private bool _GridMode = false;

    public ImagePanel(EventHub eventHub,
                      EntryCollection entryCollection,
                      ImageCache imageCache)
    {
        ImageGrid = new ImageGrid(eventHub, entryCollection, imageCache);
        ImageGrid.Dock = DockStyle.Fill;

        ImageView = new ImageView(eventHub, entryCollection, imageCache);
        ImageView.Dock = DockStyle.Fill;

        ShowGrid(true);
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

    public bool IsGrid()
    {
        return _GridMode;
    }
}
