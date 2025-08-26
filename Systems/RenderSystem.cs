using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DungeonExplorer.Entities;
using DungeonExplorer.World;
using System.Collections.Generic;

namespace DungeonExplorer.Systems
{
    /// <summary>
    /// Sistema responsabile del rendering di tutte le entità e del mondo
    /// </summary>
    public class RenderSystem
    {
        private const int TILE_SIZE = 32;

        /// <summary>
        /// Renderizza una singola entità
        /// </summary>
        public void RenderEntity(SpriteBatch spriteBatch, Entity entity)
        {
            if (entity?.Render?.Texture != null && entity.Transform != null)
            {
                var color = entity.Render.Color;
                var origin = entity.Render.Origin;
                var scale = entity.Transform.Scale;
                var rotation = entity.Transform.Rotation;

                spriteBatch.Draw(
                    entity.Render.Texture,
                    entity.Transform.Position,
                    null,
                    color,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    entity.Render.LayerDepth
                );

                // Render health bar per entità che hanno salute
                if (entity.Health != null && entity.Health.CurrentHealth < entity.Health.MaxHealth)
                {
                    RenderHealthBar(spriteBatch, entity);
                }

                // Debug rendering se abilitato
                if (entity.Render.ShowDebugInfo)
                {
                    RenderEntityDebugInfo(spriteBatch, entity);
                }
            }
        }

        /// <summary>
        /// Renderizza il dungeon completo
        /// </summary>
        public void RenderDungeon(SpriteBatch spriteBatch, Dungeon dungeon, Dictionary<string, Texture2D> textures)
        {
            if (dungeon?.Tiles == null) return;

            int width = dungeon.Width;
            int height = dungeon.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = dungeon.Tiles[x, y];
                    RenderTile(spriteBatch, tile, x, y, textures);
                }
            }

            // Render room debug info se abilitato
            if (dungeon.ShowDebugInfo)
            {
                RenderDungeonDebugInfo(spriteBatch, dungeon);
            }
        }

        /// <summary>
        /// Renderizza un singolo tile
        /// </summary>
        private void RenderTile(SpriteBatch spriteBatch, Tile tile, int x, int y,
            Dictionary<string, Texture2D> textures)
        {
            if (tile == null) return;

            var position = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
            Texture2D texture = null;
            Color color = Color.White;

            // Determina la texture basata sul tipo di tile
            switch (tile.Type)
            {
                case TileType.Floor:
                    texture = textures.ContainsKey("floor") ? textures["floor"] : null;
                    break;
                case TileType.Wall:
                    texture = textures.ContainsKey("wall") ? textures["wall"] : null;
                    break;
                case TileType.Door:
                    texture = textures.ContainsKey("door")
                        ? textures["door"]
                        : (textures.ContainsKey("floor") ? textures["floor"] : null);
                    color = Color.Brown;
                    break;
                default:
                    return; // Non renderizza tile vuoti
            }

            if (texture != null)
            {
                spriteBatch.Draw(texture, position, color);
            }

            // Rendering speciale per tile speciali
            if (tile.IsStartPosition)
            {
                // Indica la posizione di partenza con un overlay verde
                spriteBatch.Draw(texture, position, Color.Green * 0.3f);
            }
            else if (tile.IsEndPosition)
            {
                // Indica la posizione finale con un overlay rosso
                spriteBatch.Draw(texture, position, Color.Red * 0.3f);
            }
        }

        /// <summary>
        /// Renderizza la barra della salute di un'entità
        /// </summary>
        private void RenderHealthBar(SpriteBatch spriteBatch, Entity entity)
        {
            if (entity.Health == null) return;

            var position = entity.Transform.Position;
            var healthBarWidth = 30;
            var healthBarHeight = 4;
            var healthBarOffset = new Vector2(-healthBarWidth * 0.5f, -40);

            var healthBarPosition = position + healthBarOffset;
            var healthPercentage = (float)entity.Health.CurrentHealth / entity.Health.MaxHealth;

            // Background della barra (rosso)
            var backgroundRect = new Rectangle(
                (int)healthBarPosition.X,
                (int)healthBarPosition.Y,
                healthBarWidth,
                healthBarHeight
            );

            // Barra della salute (verde/giallo/rosso basato sulla percentuale)
            var healthRect = new Rectangle(
                (int)healthBarPosition.X,
                (int)healthBarPosition.Y,
                (int)(healthBarWidth * healthPercentage),
                healthBarHeight
            );

            Color healthColor = Color.Green;
            if (healthPercentage < 0.3f)
                healthColor = Color.Red;
            else if (healthPercentage < 0.6f)
                healthColor = Color.Yellow;

            // Crea texture pixel se non disponibile (metodo helper)
            var pixelTexture = CreatePixelTexture(spriteBatch.GraphicsDevice);

            spriteBatch.Draw(pixelTexture, backgroundRect, Color.DarkRed);
            spriteBatch.Draw(pixelTexture, healthRect, healthColor);
        }

        /// <summary>
        /// Renderizza informazioni di debug per un'entità
        /// </summary>
        private void RenderEntityDebugInfo(SpriteBatch spriteBatch, Entity entity)
        {
            var pixelTexture = CreatePixelTexture(spriteBatch.GraphicsDevice);

            // Bounding box
            var bounds = entity.GetBounds();
            var boundsColor = Color.Yellow * 0.5f;

            // Top
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 1), boundsColor);
            // Bottom
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Bottom - 1, bounds.Width, 1), boundsColor);
            // Left
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, 1, bounds.Height), boundsColor);
            // Right
            spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - 1, bounds.Top, 1, bounds.Height), boundsColor);

            // Centro dell'entità
            var center = entity.Transform.Position;
            spriteBatch.Draw(pixelTexture, new Rectangle((int)center.X - 2, (int)center.Y - 2, 4, 4), Color.Red);
        }

        /// <summary>
        /// Renderizza informazioni di debug per il dungeon
        /// </summary>
        private void RenderDungeonDebugInfo(SpriteBatch spriteBatch, Dungeon dungeon)
        {
            var pixelTexture = CreatePixelTexture(spriteBatch.GraphicsDevice);

            // Renderizza i contorni delle stanze
            foreach (var room in dungeon.GetRooms())
            {
                var roomBounds = new Rectangle(
                    room.X * TILE_SIZE,
                    room.Y * TILE_SIZE,
                    room.Width * TILE_SIZE,
                    room.Height * TILE_SIZE
                );

                var outlineColor = Color.Cyan * 0.7f;

                // Top
                spriteBatch.Draw(pixelTexture, new Rectangle(roomBounds.Left, roomBounds.Top, roomBounds.Width, 2),
                    outlineColor);
                // Bottom
                spriteBatch.Draw(pixelTexture,
                    new Rectangle(roomBounds.Left, roomBounds.Bottom - 2, roomBounds.Width, 2), outlineColor);
                // Left
                spriteBatch.Draw(pixelTexture, new Rectangle(roomBounds.Left, roomBounds.Top, 2, roomBounds.Height),
                    outlineColor);
                // Right
                spriteBatch.Draw(pixelTexture,
                    new Rectangle(roomBounds.Right - 2, roomBounds.Top, 2, roomBounds.Height), outlineColor);
            }
        }

        /// <summary>
        /// Renderizza un'entità con effetti speciali
        /// </summary>
        public void RenderEntityWithEffects(SpriteBatch spriteBatch, Entity entity, Color? tintColor = null,
            float flashIntensity = 0f)
        {
            if (entity?.Render?.Texture == null || entity.Transform == null) return;

            Color finalColor = entity.Render.Color;

            // Applica tint color se specificato
            if (tintColor.HasValue)
            {
                finalColor = Color.Lerp(finalColor, tintColor.Value, 0.5f);
            }

            // Applica effetto flash
            if (flashIntensity > 0)
            {
                finalColor = Color.Lerp(finalColor, Color.White, flashIntensity);
            }

            var origin = entity.Render.Origin;
            var scale = entity.Transform.Scale;
            var rotation = entity.Transform.Rotation;

            spriteBatch.Draw(
                entity.Render.Texture,
                entity.Transform.Position,
                null,
                finalColor,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                entity.Render.LayerDepth
            );
        }

        /// <summary>
        /// Renderizza un gruppo di entità con culling automatico
        /// </summary>
        public void RenderEntities(SpriteBatch spriteBatch, IEnumerable<Entity> entities, Rectangle visibleArea)
        {
            foreach (var entity in entities)
            {
                // Culling: renderizza solo le entità visibili
                if (IsEntityVisible(entity, visibleArea))
                {
                    RenderEntity(spriteBatch, entity);
                }
            }
        }

        /// <summary>
        /// Controlla se un'entità è visibile nell'area specificata
        /// </summary>
        private bool IsEntityVisible(Entity entity, Rectangle visibleArea)
        {
            if (entity?.Transform == null) return false;

            var entityBounds = entity.GetBounds();
            return visibleArea.Intersects(entityBounds);
        }

        /// <summary>
        /// Crea una texture pixel per il rendering di forme primitive
        /// </summary>
        private Texture2D CreatePixelTexture(GraphicsDevice graphicsDevice)
        {
            // Questo è un metodo helper che dovrebbe essere cached
            // In una implementazione reale, questa texture dovrebbe essere creata una volta
            // e riutilizzata
            var texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            return texture;
        }

        /// <summary>
        /// Renderizza testo del mondo (testo che segue la camera)
        /// </summary>
        public void RenderWorldText(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 worldPosition,
            Color color)
        {
            if (font != null && !string.IsNullOrEmpty(text))
            {
                spriteBatch.DrawString(font, text, worldPosition, color);
            }
        }

        /// <summary>
        /// Renderizza particelle o effetti speciali
        /// </summary>
        public void RenderParticleEffect(SpriteBatch spriteBatch, Vector2 position, Texture2D texture, Color color,
            float scale = 1.0f, float rotation = 0f)
        {
            if (texture != null)
            {
                var origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
                spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0.9f);
            }
        }
    }
}