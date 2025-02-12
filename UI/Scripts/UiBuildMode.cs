using Godot;

public partial class UiBuildMode : UiControl
{
	[Export]
	public Label CurrentModeLabel { get; set; }

	[Export]
	public UiBuildModeList BuildModeInventoryItemList { get; set; }

	[Export]
	public HBoxContainer ActionParent { get; set; }

	[Export]
	public HBoxContainer SwitchModeParent { get; set; }

	[Export]
	public HBoxContainer EnableSnappingParent { get; set; }

	[Export]
	public HBoxContainer ExitBuildModeParent { get; set; }

	[Export]
	public Button UseActionButton { get; set; }

	[Export]
	public Button SwitchModeButton { get; set; }

	[Export]
	public Button EnableSnappingButton { get; set; }

	[Export]
	public Button ExitBuildModeButton { get; set; }

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
		SelectFirstRow();
		//Position = position;
		IsOpen = true;
		return true;
	}

	public void Close()
	{
		if (BuildModeInventoryItemList is null) {
			return;
		}
		Position = new Vector2(1000, 1000);
		IsOpen = false;
	}

	public void SelectFirstRow()
	{
		TreeItem listRoot = BuildModeInventoryItemList.GetRoot();
		if (listRoot.GetChildren().Count > 0) {
			TreeItem firstItem = listRoot.GetChildren()[0];
			BuildModeInventoryItemList.GrabFocus();
			BuildModeInventoryItemList.SetSelected(firstItem, 0);
			firstItem.Select(0);
		}
	}

	public TreeItem SelectedItem()
	{
		return BuildModeInventoryItemList.GetSelected();
	}
}
