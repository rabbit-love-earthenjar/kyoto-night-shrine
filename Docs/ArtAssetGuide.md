# Art Asset Guide

## Folder Layout
- `Assets/Sprites/Environment/Platforms/`: platform tiles and platform pieces.
- `Assets/Sprites/Environment/Backgrounds/`: background images and parallax layers.
- `Assets/Sprites/Items/`: pickups, charms, shards, hearts, and other small item sprites.
- `Assets/Art/Spritesheets/Kenney/`: imported Kenney character or source spritesheet packs kept for reference and future slicing.
- `Assets/Tilemaps/Kenney/`: imported Kenney RPG/tilemap packs, including source packages, exported tile sheets, sliced tiles, and Tiled examples.

## External Asset Notes
- Keep downloaded external art grouped by provider and pack name so licensing and source context stay visible.
- Current Kenney packs are CC0 according to their included `License.txt` files. Credit is not required, but keeping the license files with the imported package is recommended.
- Do not mix external source package previews, sample images, and Tiled examples into current gameplay folders until a scene or prefab actually uses them.
- Do not place low-resolution external tile sprites directly into high-detail cafe presentation scenes unless they are restyled, upscaled, or otherwise matched to the current cafe background and character art. Use them first for graybox layout, hidden prototypes, or reference.

## Naming Rules
Use lowercase words separated by underscores.

Recommended examples:
- `platform_stone_tile_01.png`
- `platform_wood_tile_01.png`
- `platform_cloud_spiritual_01.png`
- `torii_post_01.png`
- `torii_beam_top_01.png`
- `item_spirit_shard_01.png`
- `item_heart_01.png`
- `bg_night_shrine_approach_01.png`

## Recommended Sizes
These are starting targets for the prototype. They can change after we settle the final pixel scale.

- Stone platform tile: `32x32 px`
- Wooden platform tile: `32x32 px`
- Spiritual cloud platform tile: `32x16 px` or `64x16 px`
- Torii/end gate post: `16x64 px`
- Torii/end gate beam: `64x16 px`
- Small pickups, such as Faith Point icons or Heart: `16x16 px`
- Backgrounds: at least `1920x1080 px` for painted backgrounds, or layered pixel-art panels sized to the camera view.

## Import Notes
- Use transparent PNG for sprites that should not have a white box.
- Keep platform sprites simple and readable before adding detail.
- Keep collider shapes simple in Unity even if the sprite has decorative edges.
- Use placeholder colors consistently until the final platform art is ready:
  - Normal stone platforms: dark stone.
  - Wooden platforms: brown.
  - Spiritual cloud platforms: pale blue.
  - Torii/end gate pieces: red.
