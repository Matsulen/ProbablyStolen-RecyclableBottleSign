-- Builds preview.png (mod menu / Workshop preview) from the sign sprite, scaled 8x on a dark backdrop.
-- params: sign (input png), out (output png)
local src = app.open(app.params["sign"])
local img = Image(src.spec)
img:drawSprite(src, 1)
local S, pad = 8, 16
local size = 32 * S + pad * 2
local out = Image(size, size, ColorMode.RGB)
out:clear(app.pixelColor.rgba(0x29, 0x22, 0x34, 255))
for y = 0, 32 * S - 1 do
  for x = 0, 32 * S - 1 do
    local c = img:getPixel(x // S, y // S)
    if app.pixelColor.rgbaA(c) > 8 then out:drawPixel(pad + x, pad + y, c) end
  end
end
local spr = Sprite(size, size, ColorMode.RGB)
spr.cels[1].image = out
spr:saveCopyAs(app.params["out"])
