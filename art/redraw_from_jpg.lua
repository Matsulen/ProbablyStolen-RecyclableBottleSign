-- Redraws a lossy 32x32 JPG of the sign as a clean PNG.
-- The frame comes from variant A (pixel exact); every panel pixel is snapped to the nearest sign colour.
-- params: jpg, base (variant A png), out_png, out_ase, out_preview, out_txt
local P = app.params
local js = app.open(P["jpg"])
local jimg = Image(js.width, js.height, ColorMode.RGB)
jimg:drawSprite(js, 1)
local bs = app.open(P["base"])
local base = Image(32, 32, ColorMode.RGB)
base:drawSprite(bs, 1)

local txt = io.open(P["out_txt"], "w")
txt:write("jpg size " .. js.width .. "x" .. js.height .. "\n")

local pc = app.pixelColor
local panelColors = {
  { "d", 0xd5, 0xd2, 0xb8 },
  { "c", 0x79, 0xb0, 0x6a },
  { "b", 0x3a, 0x74, 0x46 },
  { "e", 0x22, 0x46, 0x3a },
}
local function inPanel(x, y)
  if y >= 8 and y <= 23 then return x >= 6 and x <= 25 end
  if y == 7 or y == 24 then return x >= 7 and x <= 24 end
  return false
end

local out = Image(32, 32, ColorMode.RGB)
local sx, sy = js.width / 32, js.height / 32

-- Pass 1: which panel pixels are neon tube. JPG noise makes the two dark greens
-- unreliable, so only the tube is read from the picture.
local tube = {}
for y = 7, 24 do
  for x = 6, 25 do
    if inPanel(x, y) then
      local c = jimg:getPixel(math.floor((x + 0.5) * sx), math.floor((y + 0.5) * sy))
      local r, g, b = pc.rgbaR(c), pc.rgbaG(c), pc.rgbaB(c)
      local best, bestD = nil, 1e9
      for _, p in ipairs(panelColors) do
        local d = (r - p[2]) ^ 2 + (g - p[3]) ^ 2 + (b - p[4]) ^ 2
        if d < bestD then best, bestD = p, d end
      end
      if best[1] == "d" or best[1] == "c" then tube[y * 32 + x] = true end
    end
  end
end

-- Pass 2: tube in cream, a one pixel dark-green halo around it, deep green elsewhere.
for y = 0, 31 do
  local line = ""
  for x = 0, 31 do
    if inPanel(x, y) then
      local best
      if tube[y * 32 + x] then
        best = panelColors[1]
      else
        local near = false
        for dy = -1, 1 do
          for dx = -1, 1 do
            if tube[(y + dy) * 32 + x + dx] then near = true end
          end
        end
        best = near and panelColors[3] or panelColors[4]
      end
      out:drawPixel(x, y, pc.rgba(best[2], best[3], best[4], 255))
      line = line .. best[1]
    else
      out:drawPixel(x, y, base:getPixel(x, y))
      line = line .. "."
    end
  end
  txt:write(line .. "\n")
end
txt:close()

local spr = Sprite(32, 32, ColorMode.RGB)
spr.cels[1].image = out
spr.layers[1].name = "sign_recyclable"
spr:saveAs(P["out_ase"])
spr:saveCopyAs(P["out_png"])

-- preview: uploaded JPG (left) next to the redrawn PNG (right), 8x
local S, pad = 8, 4
local pw, ph = 2 * 32 + 3 * pad, 32 + 2 * pad
local big = Image(pw * S, ph * S, ColorMode.RGB)
big:clear(pc.rgba(0x3b, 0x3a, 0x44, 255))
for y = 0, 32 * S - 1 do
  for x = 0, 32 * S - 1 do
    local jc = jimg:getPixel(math.floor((x // S + 0.5) * sx), math.floor((y // S + 0.5) * sy))
    big:drawPixel(pad * S + x, pad * S + y, pc.rgba(pc.rgbaR(jc), pc.rgbaG(jc), pc.rgbaB(jc), 255))
    local oc = out:getPixel(x // S, y // S)
    if pc.rgbaA(oc) > 8 then big:drawPixel((2 * pad + 32) * S + x, pad * S + y, oc) end
  end
end
local ps = Sprite(pw * S, ph * S, ColorMode.RGB)
ps.cels[1].image = big
ps:saveCopyAs(P["out_preview"])
