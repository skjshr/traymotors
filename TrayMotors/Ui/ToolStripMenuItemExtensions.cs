namespace TrayMotors;

internal static class ToolStripMenuItemExtensions
{
    public static void SetChecked(this ToolStripMenuItem item, bool isChecked, string uncheckedText, string checkedText)
    {
        item.Checked = isChecked;
        item.Text = isChecked ? checkedText : uncheckedText;
    }
}
