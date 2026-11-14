-- Builds the 32x32 "recyclable" neon sign in the style of the vanilla 2x2 signs.
-- params: out_png, out_ase, out_preview, ref_dir
local P = app.params

-- Frame copied from the vanilla 2x2 sign layout (outline / dark / mid / tube).
local frame = {
"################################",
"#####aaaaaaaaaaaaaaaaaaaaaa#####",
"###aaabbbbbbbbbbbbbbbbbbbbaaa###",
"##aabbbccccccccccccccccccbbbaa##",
"#aabbccddddddddddddddddddccbbaa#",
"#abbcddccccccccccccccccccddcbba#",
"aabcdccbbbbbbbbbbbbbbbbbbccdcbaa",
"abbcdcbbbbbbbbbbbbbbbbbbbbcdcbba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abcdcbbbbbbbbbbbbbbbbbbbbbbcdcba",
"abbcdcbbbbbbbbbbbbbbbbbbbbcdcbba",
"aabcdccbbbbbbbbbbbbbbbbbbccdcbaa",
"#abbcddccccccccccccccccccddcbba#",
"#aabbccddddddddddddddddddccbbaa#",
"##aabbbccccccccccccccccccbbbaa##",
"###aaabbbbbbbbbbbbbbbbbbbbaaa###",
"#####aaaaaaaaaaaaaaaaaaaaaa#####",
"################################",
}

-- Recycling symbol, 16x16, placed at (8,8). X = neon tube.
local icon = {
"......XXX.......",
".....XX.XX......",
"....XX...XX.....",
"...XX.....XX....",
".........XXXXX..",
"....XX....XXXX..",
"...XXX.....XXX..",
"..XXXX......XX..",
".XXXXX..........",
".XX..........XX.",
".XX.......X..XX.",
"XX.......XX...XX",
"XXXXXX..XXXXXXXX",
"XXXXXX..XXXXXXXX",
".........XX.....",
"..........X.....",
}
local IX, IY = 8, 8

local function rgba(hex, a)
  local r = tonumber(hex:sub(1, 2), 16)
  local g = tonumber(hex:sub(3, 4), 16)
  local b = tonumber(hex:sub(5, 6), 16)
  return app.pixelColor.rgba(r, g, b, a or 255)
end

local pal = {
  ["#"] = app.pixelColor.rgba(0, 0, 0, 1), -- vanilla signs use alpha-1 filler
  a = rgba("292234"),  -- outline (vanilla)
  b = rgba("3a7446"),  -- dark green
  c = rgba("79b06a"),  -- mid green
  d = rgba("d5d2b8"),  -- cream tube (vanilla)
  e = rgba("27513a"),  -- deep green shade
  f = rgba("2c3d3c"),  -- shade edge
}

-- grid[y][x] = palette key
local grid = {}
for y = 0, 31 do
  grid[y] = {}
  for x = 0, 31 do grid[y][x] = frame[y + 1]:sub(x + 1, x + 1) end
end

local function inPanel(x, y)
  if y >= 8 and y <= 23 then return x >= 6 and x <= 25 end
  if y == 7 or y == 24 then return x >= 7 and x <= 24 end
  return false
end

-- icon tube
local tube = {}
for y = 0, 15 do
  for x = 0, 15 do
    if icon[y + 1]:sub(x + 1, x + 1) == "X" then
      grid[IY + y][IX + x] = "d"
      tube[#tube + 1] = { IX + x, IY + y }
    end
  end
end

local function distToTube(x, y)
  local best = 99
  for _, p in ipairs(tube) do
    local dx, dy = math.abs(p[1] - x), math.abs(p[2] - y)
    local d = math.max(dx, dy)
    if d < best then best = d end
  end
  return best
end

-- glow outline around the tube + diagonal corner shading like the vanilla signs
for y = 7, 24 do
  for x = 6, 25 do
    if inPanel(x, y) and grid[y][x] == "b" then
      local dist = distToTube(x, y)
      local s = (x - 6) + (y - 7)
      if dist == 1 then
        grid[y][x] = "c"
      elseif dist >= 3 and s <= 9 then
        grid[y][x] = "e"
      elseif dist >= 3 and s == 10 then
        grid[y][x] = "f"
      end
    end
  end
end

local spr = Sprite(32, 32, ColorMode.RGB)
local img = Image(32, 32, ColorMode.RGB)
for y = 0, 31 do
  for x = 0, 31 do img:drawPixel(x, y, pal[grid[y][x]]) end
end
spr.cels[1].image = img
spr.layers[1].name = "sign_recyclable"
spr:saveAs(P["out_ase"])
spr:saveCopyAs(P["out_png"])

-- Preview: vanilla signs next to the new one, scaled 8x on a dark backdrop.
local refs = { "household_sign", "material_sign", "food_sign", "berry_sign" }
local S, pad = 8, 4
local n = #refs + 1
local pw, ph = n * (32 + pad) + pad, 32 + pad * 2
local small = Image(pw, ph, ColorMode.RGB)
small:clear(rgba("3b3a44"))
local function blit(src, ox, oy)
  for y = 0, src.height - 1 do
    for x = 0, src.width - 1 do
      local c = src:getPixel(x, y)
      if app.pixelColor.rgbaA(c) > 8 then small:drawPixel(ox + x, oy + y, c) end
    end
  end
end
for i, name in ipairs(refs) do
  local rs = app.open(P["ref_dir"] .. "/" .. name .. ".png")
  local ri = Image(rs.spec)
  ri:drawSprite(rs, 1)
  blit(ri, pad + (i - 1) * (32 + pad), pad)
  rs:close()
end
blit(img, pad + (n - 1) * (32 + pad), pad)
local big = Image(pw * S, ph * S, ColorMode.RGB)
for y = 0, ph * S - 1 do
  for x = 0, pw * S - 1 do
    big:drawPixel(x, y, small:getPixel(x // S, y // S))
  end
end
local ps = Sprite(pw * S, ph * S, ColorMode.RGB)
ps.cels[1].image = big
ps:saveCopyAs(P["out_preview"])
