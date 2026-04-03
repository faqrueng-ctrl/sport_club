using System.Drawing;
using System.Windows.Forms;

namespace SportClubApp.Utils
{
    public static class UiTheme
    {
        public static void Apply(Control root)
        {
            root.Font = new Font("Segoe UI", 10f);
            root.BackColor = Color.White;
        }
    }
}
