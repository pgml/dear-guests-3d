using Godot;

public partial class UiBeltItem : Button
{
	[Export]
	public Label InputLabel { get; set; }

	[Export]
	public TextureRect ItemSelectedTexture { get; set; }

	[Export]
	public TextureRect ItemTexture { get; set; }

	[Export]
	public TextureRect ItemAmountBg { get; set; }

	[Export]
	public Label AmountLabel { get; set; }

	public bool IsSelected { get; set; }

	public int InventoryIndex { get; set; }

	public override void _Ready()
	{
		ItemTexture.SetSize(new Vector2(32f, 32f), true);
		ItemTexture.ClipContents = true;
		ItemTexture.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		ItemTexture.StretchMode = TextureRect.StretchModeEnum.Keep;
	}

	public override void _Process(double delta)
	{
		ItemSelectedTexture.Visible = false;
		if (IsSelected) {
			ItemSelectedTexture.Visible = true;
		}
	}

	public UiBeltItem Populate(InventoryItemResource slotItem)
	{
		ItemTexture.Visible = false;
		ItemAmountBg.Visible = false;
		AmountLabel.Visible = false;

		if (slotItem is not null) {
			ItemTexture.Visible = true;
			ItemTexture.Texture = slotItem.ItemResource.Sprite;

			if (slotItem.Amount > 0 && slotItem.ItemResource.IsUseable) {
				ItemAmountBg.Visible = true;
				AmountLabel.Visible = true;
				AmountLabel.Text = $"{slotItem.Amount}";
			}
		}

		return this;
	}
}
