using Godot;

public partial class UiBuildMode : UiControl
{
	[Export]
	public Label Label { get; set; }

	[Export]
	public UiBuildModeList BuildModeInventoryItemList { get; set; }

	public override void _Ready()
	{
		base._Ready();
	}

	//public void Open(Vector2 position)
	public bool Open()
	{
		TreeItem listRoot = BuildModeInventoryItemList.GetRoot();
		if (listRoot.GetChildren().Count <= 0) {
			return false;
		}

		BuildModeInventoryItemList.PopulateList();
		TreeItem firstItem = listRoot.GetChildren()[0];
		BuildModeInventoryItemList.GrabFocus();
		BuildModeInventoryItemList.SetSelected(firstItem, 0);
		firstItem.Select(0);
		//Position = position;
		IsOpen = true;

		return true;
	}

	public void Close()
	{
		if (BuildModeInventoryItemList is null) {
			return;
		}
		Label.Text = "";
		Position = new Vector2(1000, 1000);
		IsOpen = false;
	}

	public TreeItem SelectedItem()
	{
		return BuildModeInventoryItemList.GetSelected();
	}
}
