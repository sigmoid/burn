using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using peridot.EntityComponentScene.Physics;
using peridot.Physics;
using Peridot;
using Peridot.Components;
using Peridot.EntityComponentScene.Serialization;
using Peridot.Graphics;

public class ItemPlacementManager
{
    private PlayerInventory _inventory;
    private InventoryItemType? _currentItemType = null;
    private Entity? _currentItemEntity = null;

    public ItemPlacementManager(PlayerInventory inventory)
    {
        _inventory = inventory;
        inventory.OnItemSelected += SetCursorItem;
    }
    public void SetCursorItem(InventoryItemType itemType)
    {
        _currentItemType = itemType;

        UpdateCursor();
    }

    public void Update()
    {
        if (_currentItemEntity != null)
        {
            var mousePos = Core.InputManager.GetMousePosition() * new Microsoft.Xna.Framework.Vector2(Core.ScreenWidth, Core.ScreenHeight);
            var worldPos = Core.Camera.ScreenToWorld(mousePos);
            var spriteSize = new Vector2(_currentItemEntity.GetComponent<SpriteComponent>().Sprite.Width, _currentItemEntity.GetComponent<SpriteComponent>().Sprite.Height);
            _currentItemEntity.Position = worldPos;
        }

        if (Core.InputManager.GetButton("LeftMouse").IsPressed)
        {
            if (_currentItemEntity != null)
            {
                CreateEntity();
                _inventory.RemoveItem(_currentItemType.Value, 1);
                UpdateCursor();
            }
        }
    }

    private void UpdateCursor()
    {
        if (_currentItemType == null)
        {
            if (_currentItemEntity != null)
            {
                Core.CurrentScene.RemoveEntity(_currentItemEntity);
                _currentItemEntity = null;
            }
            return;
        }

        if (_inventory.GetQuantity(_currentItemType.Value) > 0)
        {
            if (_currentItemEntity != null)
            {
                Core.CurrentScene.RemoveEntity(_currentItemEntity);
                _currentItemEntity = null;
            }


            var sprite = new Sprite(Core.Content.Load<Texture2D>(InventoryItemRegistry.Items[_currentItemType.Value].BurnablePath));
            sprite.Color = new Microsoft.Xna.Framework.Color(255, 255, 255, 100);
            var spriteComponent = new SpriteComponent(sprite);

            var Entity = new Entity();
            Entity.Name = "CursorItem";
            Entity.AddComponent(spriteComponent);

            Core.CurrentScene.AddEntity(Entity);
            _currentItemEntity = Entity;
        }
        else
        {
            if (_currentItemEntity != null)
            {
                Core.CurrentScene.RemoveEntity(_currentItemEntity);
                _currentItemEntity = null;
            }
        }
    }
    
    private void CreateEntity()
    {
        var mousePos = Core.InputManager.GetMousePosition() * new Microsoft.Xna.Framework.Vector2(Core.ScreenWidth, Core.ScreenHeight);

        var spriteSize = new Vector2(_currentItemEntity.GetComponent<SpriteComponent>().Sprite.Width, _currentItemEntity.GetComponent<SpriteComponent>().Sprite.Height);
        var placePosition = mousePos - spriteSize/2;
		var newEntity = EntityFactory.FromString(
			$"""
                <Entity Name="BurnableSprite">
                    <Position>
                    <X>{placePosition.X}</X>
                    <Y>{placePosition.Y}</Y>
                    </Position>
                    <Component Type="BurnableSpriteComponent">
                    <Property Name="spritePath" Value="{InventoryItemRegistry.Items[_currentItemType.Value].BurnablePath}" />
                    <Property Name="burnRate" Value="0.1" />
                    </Component>
                </Entity>
            """
		);

		var polygonColliderComponent = new PolygonColliderComponent();
		polygonColliderComponent.Vertices = new System.Collections.Generic.List<Vector2>
			{
				PhysicsSystem.ToSimUnits(new Vector2(0, 0)),
				PhysicsSystem.ToSimUnits(new Vector2(128, 0)),
				PhysicsSystem.ToSimUnits(new Vector2(128, 128)),
				PhysicsSystem.ToSimUnits(new Vector2(0, 128))
			};
		newEntity.AddComponent(polygonColliderComponent);
		newEntity.AddComponent(new RigidbodyComponent(BodyType.Dynamic));
		Core.CurrentScene.AddEntity(newEntity);
	}
}