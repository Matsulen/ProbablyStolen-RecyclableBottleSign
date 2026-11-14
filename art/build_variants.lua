-- Builds four candidate "recyclable" neon signs (32x32) plus a comparison sheet.
-- params: out_dir, ref_dir
local P = app.params

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

-- Icons: X = neon tube. Each is centred in the 20x18 panel.
local variants = {
  { name = "A", icon = {
"........XX........",
".......XXXX.......",
"......XX..XX......",
".....XX....XX.....",
"............XX....",
".............XX...",
"...XX......XXXXXX.",
"..XXXX......XXXX..",
".XXXXXX......XX...",
"...XX.............",
"..XX......X.......",
".XX......XX....XX.",
".XXXXX..XXXXXXXXX.",
".XXXXX..XXXXXXXXX.",
".........XX.......",
"..........X.......",
  } },
  { name = "B", icon = {
"........XX........",
".......XXXX.......",
"......XX..XX......",
".....XX....XX.....",
"............XX....",
".............XX...",
"...XX.....XX.XX.XX",
"..XXXX.....XXXXXX.",
".XXXXXX.....XXXX..",
"XX.XX.XX.....XX...",
"..XX......XX......",
".XX......XX....XX.",
".XXXX..XXXXXXXXXX.",
".XXXX..XXXXXXXXXX.",
".........XX.......",
"..........XX......",
  } },
  { name = "C", icon = {
"......XXXXX.....",
"....XXXXXXXXX...",
"...XX......XXX..",
"............XX..",
"..X.......XXXXXX",
".XXX.......XXXX.",
"XXXXX.......XX..",
".XX.............",
".XX...........XX",
".XX...........XX",
".XX.....X....XX.",
"..XX...XX..XXX..",
"...X..XXXXXXX...",
"......XXXXXX....",
".......XX.......",
"........X.......",
  } },
  { name = "D", icon = {
"....XXXXXX........",
"....XXXXXX........",
"......XXXX.XX.....",
".....XX.XX..XX....",
"....XX..XX...XX.XX",
"...XX...XX....XXXX",
"..XX...........XXX",
".XX.............XX",
"XX..........XXXXXX",
"............XXXXXX",
"....X.............",
"...XX.............",
"..XXXXXXXXXXXXXX..",
"..XXXXXXXXXXXXXX..",
"...XX.............",
"....X.............",
  } },
}

local function rgba(hex, a)
  return app.pixelColor.rgba(tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16), a or 255)
end

local pal = {
  ["#"] = app.pixelColor.rgba(0, 0, 0, 1),
  a = rgba("292234"),  -- outline (vanilla)
  b = rgba("3a7446"),  -- dark green
  c = rgba("79b06a"),  -- mid green
  d = rgba("d5d2b8"),  -- cream tube (vanilla)
  e = rgba("22463a"),  -- deep green panel
}

local function inPanel(x, y)
  if y >= 8 and y <= 23 then return x >= 6 and x <= 25 end
  if y == 7 or y == 24 then return x >= 7 and x <= 24 end
  return false
end

local function build(icon)
  local grid = {}
  for y = 0, 31 do
    grid[y] = {}
    for x = 0, 31 do grid[y][x] = frame[y + 1]:sub(x + 1, x + 1) end
  end
  local w, h = #icon[1], #icon
  local ix = 6 + (20 - w) // 2
  local iy = 7 + (18 - h) // 2
  local tube = {}
  for y = 0, h - 1 do
    for x = 0, w - 1 do
      if icon[y + 1]:sub(x + 1, x + 1) == "X" then tube[(iy + y) * 32 + ix + x] = true end
    end
  end
  for y = 7, 24 do
    for x = 6, 25 do
      if inPanel(x, y) then
        if tube[y * 32 + x] then
          grid[y][x] = "d"
        else
          -- uniform deep panel, with a one pixel dark-green halo hugging the tube
          local near = false
          for dy = -1, 1 do
            for dx = -1, 1 do
              if tube[(y + dy) * 32 + x + dx] then near = true end
            end
          end
          grid[y][x] = near and "b" or "e"
        end
      end
    end
  end
  local img = Image(32, 32, ColorMode.RGB)
  for y = 0, 31 do
    for x = 0, 31 do img:drawPixel(x, y, pal[grid[y][x]]) end
  end
  return img
end

local images = {}
for i, v in ipairs(variants) do
  local img = build(v.icon)
  images[i] = img
  local spr = Sprite(32, 32, ColorMode.RGB)
  spr.cels[1].image = img
  spr.layers[1].name = "sign_recyclable_" .. v.name
  spr:saveAs(P["out_dir"] .. "/sign_recyclable_" .. v.name .. ".aseprite")
  spr:saveCopyAs(P["out_dir"] .. "/sign_recyclable_" .. v.name .. ".png")
  spr:close()
end

-- Sheet: two vanilla signs for reference, then A B C D. 8x scale.
local refs = { "household_sign", "berry_sign" }
local S, pad = 8, 4
local n = #refs + #images
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
for i, img in ipairs(images) do
  blit(img, pad + (#refs + i - 1) * (32 + pad), pad)
end
local big = Image(pw * S, ph * S, ColorMode.RGB)
for y = 0, ph * S - 1 do
  for x = 0, pw * S - 1 do big:drawPixel(x, y, small:getPixel(x // S, y // S)) end
end
local ps = Sprite(pw * S, ph * S, ColorMode.RGB)
ps.cels[1].image = big
ps:saveCopyAs(P["out_dir"] .. "/variants_compare.png")
