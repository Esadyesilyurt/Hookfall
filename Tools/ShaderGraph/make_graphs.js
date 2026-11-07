// Hookfall - builds the toon and outline Shader Graphs from Unity's own Shader Graph samples.
// Run from the project root: node Tools/ShaderGraph/make_graphs.js
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
//
// SG_Toon     = the "Custom Lighting Toon" sample (Unity Shader Graph package) + a Base Color tint (_BaseColor),
//               a white default Base Map, low fixed smoothness, and 3 light bands instead of 4 (GDD 8.1).
// SG_Outline  = the "Unlit Simple" template turned into an inverted hull: back faces only, vertices pushed out
//               along the normal by _OutlineWidth, flat _OutlineColor. Drawn over every opaque object by a
//               Render Objects renderer feature (set up by the Hookfall > Setup Toon Rendering menu).
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { parse, write } = require('./sglib.js');

const root = process.cwd();
const cache = path.join(root, 'Library', 'PackageCache');
const sgPackage = fs.readdirSync(cache).find(d => d.startsWith('com.unity.shadergraph@'));
const sampleDir = path.join(cache, sgPackage, 'Samples~', 'CustomLighting');
const outDir = path.join(root, 'Assets', '_Hookfall', 'Art', 'Shaders');
const libDir = path.join(outDir, 'CustomLighting');

const id = () => crypto.randomBytes(16).toString('hex');
const clone = o => JSON.parse(JSON.stringify(o));

// 1. Copy the sample's subgraphs (with their .meta files, so subgraph GUIDs match) into the project.
function copyDir(from, to) {
  fs.mkdirSync(to, { recursive: true });
  for (const entry of fs.readdirSync(from, { withFileTypes: true })) {
    if (entry.name === '.sample.json') continue;
    if (entry.isFile() && /^Custom Lighting .*\.shadergraph(\.meta)?$/.test(entry.name)) continue;   // templates, not needed
    if (entry.isFile() && /Thumbnail\.png(\.meta)?$/.test(entry.name)) continue;
    const a = path.join(from, entry.name), b = path.join(to, entry.name);
    if (entry.isDirectory()) copyDir(a, b);
    else fs.copyFileSync(a, b);
  }
}
if (!fs.existsSync(libDir)) copyDir(sampleDir, libDir);

// 3 light bands: Posterize steps 4 -> 3 in our copy of the LightToon subgraph.
const lightToonFile = path.join(libDir, 'Components', 'Core Models', 'LightToon.shadersubgraph');
const lightToon = parse(lightToonFile);
lightToon.find(o => o.m_DefaultReferenceName === '_Steps').m_Value = 3;
write(lightToonFile, lightToon);

// Helpers over one graph's object list.
function make(objs) {
  const g = objs[0];
  const find = oid => objs.find(o => o.m_ObjectId === oid);
  const api = {
    g, find,
    cloneNode(srcObjs, nodeId, x, y, extra = {}) {
      const src = srcObjs.find(o => o.m_ObjectId === nodeId);
      const n = clone(src);
      n.m_ObjectId = id();
      n.m_Group = { m_Id: '' };
      n.m_Slots = n.m_Slots.map(s => {
        const so = clone(srcObjs.find(o => o.m_ObjectId === s.m_Id));
        so.m_ObjectId = id();
        objs.push(so);
        return { m_Id: so.m_ObjectId };
      });
      n.m_DrawState.m_Position.x = x;
      n.m_DrawState.m_Position.y = y;
      Object.assign(n, extra);
      objs.push(n);
      g.m_Nodes.push({ m_Id: n.m_ObjectId });
      return n;
    },
    cloneProperty(src, name, reference, value) {
      const p = clone(src);
      p.m_ObjectId = id();
      p.m_Guid = { m_GuidSerialized: crypto.randomUUID() };
      p.m_Name = name;
      p.m_RefNameGeneratedByDisplayName = name;
      p.m_DefaultReferenceName = '_' + name.replace(/ /g, '_');
      p.m_OverrideReferenceName = reference;
      p.m_Value = value;
      objs.push(p);
      g.m_Properties.push({ m_Id: p.m_ObjectId });
      find(g.m_CategoryData[0].m_Id).m_ChildObjectList.push({ m_Id: p.m_ObjectId });
      return p;
    },
    edge(outNode, outSlot, inNode, inSlot) {
      g.m_Edges.push({ m_OutputSlot: { m_Node: { m_Id: outNode }, m_SlotId: outSlot }, m_InputSlot: { m_Node: { m_Id: inNode }, m_SlotId: inSlot } });
    },
    removeEdges(pred) { g.m_Edges = g.m_Edges.filter(e => !pred(e)); },
    removeObject(oid) {
      const o = find(oid);
      for (const s of (o.m_Slots || [])) objs.splice(objs.indexOf(find(s.m_Id)), 1);
      objs.splice(objs.indexOf(o), 1);
      g.m_Nodes = g.m_Nodes.filter(n => n.m_Id !== oid);
      g.m_Properties = g.m_Properties.filter(n => n.m_Id !== oid);
      for (const c of g.m_CategoryData) { const cat = find(c.m_Id); cat.m_ChildObjectList = cat.m_ChildObjectList.filter(n => n.m_Id !== oid); }
      api.removeEdges(e => e.m_OutputSlot.m_Node.m_Id === oid || e.m_InputSlot.m_Node.m_Id === oid);
    },
    slot(nodeId, slotId) {
      return find(nodeId).m_Slots.map(s => find(s.m_Id)).find(s => s.m_Id === slotId);
    }
  };
  return api;
}

// 2. SG_Toon
{
  const objs = parse(path.join(sampleDir, 'Custom Lighting Toon.shadergraph'));
  const t = make(objs);
  t.g.m_Path = 'Hookfall';

  const prop = name => objs.find(o => /ShaderProperty$/.test(o.m_Type) && o.m_Name === name);
  const propNode = p => objs.find(o => o.m_Type.endsWith('PropertyNode') && o.m_Property && o.m_Property.m_Id === p.m_ObjectId);
  const litToon = objs.find(o => o.m_Type.endsWith('SubGraphNode') && o.m_Name === 'Lit Toon');

  prop('Base Map').m_DefaultType = 0;              // white, so the Base Color tint is the colour
  const masks = prop('Masks');
  masks.m_Value = { m_SerializedTexture: '', m_Guid: '' };
  masks.m_DefaultType = 0;                          // white: no ambient occlusion darkening

  // Smoothness: fixed and low instead of the mask's alpha, so highlights stay small and soft.
  const SMOOTHNESS = -103177115;
  t.removeEdges(e => e.m_InputSlot.m_Node.m_Id === litToon.m_ObjectId && e.m_InputSlot.m_SlotId === SMOOTHNESS);
  t.slot(litToon.m_ObjectId, SMOOTHNESS).m_Value = 0.15;

  // Base Color tint: Base Map × _BaseColor -> Lit Toon Base Color.
  const BASE = 110148794;
  const baseEdge = t.g.m_Edges.find(e => e.m_InputSlot.m_Node.m_Id === litToon.m_ObjectId && e.m_InputSlot.m_SlotId === BASE);
  const baseSample = baseEdge.m_OutputSlot.m_Node.m_Id;
  t.removeEdges(e => e === baseEdge);

  const emissive = prop('Emissive Color');
  const baseColor = t.cloneProperty(emissive, 'Base Color', '_BaseColor', { r: 1, g: 1, b: 1, a: 1 });
  if ('m_ColorMode' in baseColor) baseColor.m_ColorMode = 0;
  const lx = litToon.m_DrawState.m_Position.x, ly = litToon.m_DrawState.m_Position.y;
  const colorNode = t.cloneNode(objs, propNode(emissive).m_ObjectId, lx - 420, ly - 160, { m_Property: { m_Id: baseColor.m_ObjectId } });
  t.find(colorNode.m_Slots[0].m_Id).m_DisplayName = 'Base Color';
  const multiplyTemplate = objs.find(o => o.m_Type.endsWith('MultiplyNode'));
  const mul = t.cloneNode(objs, multiplyTemplate.m_ObjectId, lx - 220, ly - 200);
  t.edge(baseSample, 0, mul.m_ObjectId, 0);
  t.edge(colorNode.m_ObjectId, 0, mul.m_ObjectId, 1);
  t.edge(mul.m_ObjectId, 2, litToon.m_ObjectId, BASE);

  // SG_Backdrop: the same toon look without fog, for the far scenery beyond the storm wall. Fog would turn
  // anything 400 m away into flat sky colour; pale material colours give the haze instead.
  {
    const backdrop = clone(objs);
    backdrop[0].m_Path = 'Hookfall';
    write(path.join(outDir, 'SG_Backdrop.shadergraph'), backdrop);
  }

  // Scene fog (the sample leaves it out): Lit Toon -> Fog subgraph -> Base Color, so far islands fade into the
  // pale blue haze of the abyss (GDD 8.1).
  const simple = parse(path.join(sampleDir, 'Components', 'Core Models', 'LightSimple.shadersubgraph'));
  const fogTemplate = simple.find(x => x.m_Type.endsWith('SubGraphNode') && x.m_Name === 'Fog');
  const fog = t.cloneNode(simple, fogTemplate.m_ObjectId, lx + 260, ly);
  const baseColorBlock = objs.find(x => x.m_Type.endsWith('BlockNode') && x.m_SerializedDescriptor === 'SurfaceDescription.BaseColor');
  t.removeEdges(e => e.m_InputSlot.m_Node.m_Id === baseColorBlock.m_ObjectId);
  t.edge(litToon.m_ObjectId, 1, fog.m_ObjectId, 57870196);
  t.edge(fog.m_ObjectId, 1, baseColorBlock.m_ObjectId, 0);

  write(path.join(outDir, 'SG_Toon.shadergraph'), objs);
}

// 3. SG_Outline
{
  const objs = parse(path.join(cache, sgPackage, 'GraphTemplates', 'Cross Pipeline', 'Unlit Simple.shadergraph'));
  const o = make(objs);
  o.g.m_Path = 'Hookfall';

  // Universal only: drop the HDRP target and its data objects (HDRP is not installed).
  const urp = objs.find(x => x.m_Type.endsWith('.UniversalTarget'));
  for (const x of objs.filter(x => /\.HD|BuiltinData|SystemData/.test(x.m_Type))) objs.splice(objs.indexOf(x), 1);
  o.g.m_ActiveTargets = [{ m_Id: urp.m_ObjectId }];
  urp.m_RenderFace = 1;           // back faces: the inverted hull
  urp.m_CastShadows = false;
  urp.m_ReceiveShadows = false;

  const block = name => objs.find(x => x.m_Type.endsWith('BlockNode') && x.m_SerializedDescriptor === name);
  const baseColorBlock = block('SurfaceDescription.BaseColor');
  const positionBlock = block('VertexDescription.Position');

  const multiplyTemplate = clone(objs.find(x => x.m_Type.endsWith('MultiplyNode')));
  const multiplySlots = multiplyTemplate.m_Slots.map(s => clone(o.find(s.m_Id)));
  const templates = [multiplyTemplate, ...multiplySlots];

  // Remove the texture sampling; keep the Color property as the outline colour.
  for (const n of objs.filter(x => /SampleTexture2DNode|MultiplyNode/.test(x.m_Type))) o.removeObject(n.m_ObjectId);
  const baseMap = objs.find(x => /Texture2DShaderProperty$/.test(x.m_Type));
  o.removeObject(objs.find(x => x.m_Type.endsWith('PropertyNode') && x.m_Property.m_Id === baseMap.m_ObjectId).m_ObjectId);
  o.removeObject(baseMap.m_ObjectId);
  o.g.m_Edges = [];

  const color = objs.find(x => /ColorShaderProperty$/.test(x.m_Type));
  color.m_Name = 'Outline Color';
  color.m_RefNameGeneratedByDisplayName = 'Outline Color';
  color.m_DefaultReferenceName = '_Outline_Color';
  color.m_OverrideReferenceName = '_OutlineColor';
  color.m_Value = { r: 0.11, g: 0.09, b: 0.14, a: 1 };
  const colorNode = objs.find(x => x.m_Type.endsWith('PropertyNode') && x.m_Property.m_Id === color.m_ObjectId);
  // Through the fog too, so distant outlines fade with the islands instead of floating in the haze.
  const simple = parse(path.join(sampleDir, 'Components', 'Core Models', 'LightSimple.shadersubgraph'));
  const fog = o.cloneNode(simple, simple.find(x => x.m_Type.endsWith('SubGraphNode') && x.m_Name === 'Fog').m_ObjectId, -260, 200);
  o.edge(colorNode.m_ObjectId, 0, fog.m_ObjectId, 57870196);
  o.edge(fog.m_ObjectId, 1, baseColorBlock.m_ObjectId, 0);

  // Vertex: position + normal × width, in object space.
  const lt = parse(path.join(sampleDir, 'Components', 'Core Models', 'LightToon.shadersubgraph'));
  const steps = lt.find(x => x.m_DefaultReferenceName === '_Steps');
  const width = o.cloneProperty(steps, 'Outline Width', '_OutlineWidth', 0.035);
  const stepsNode = lt.find(x => x.m_Type.endsWith('PropertyNode') && x.m_Property.m_Id === steps.m_ObjectId);
  const widthNode = o.cloneNode(lt, stepsNode.m_ObjectId, -900, -60, { m_Property: { m_Id: width.m_ObjectId } });
  o.find(widthNode.m_Slots[0].m_Id).m_DisplayName = 'Outline Width';

  const normalNode = o.cloneNode(lt, lt.find(x => x.m_Type.endsWith('NormalVectorNode')).m_ObjectId, -900, -260, { m_Space: 0 });
  const cyclo = parse(path.join(cache, fs.readdirSync(cache).find(d => d.startsWith('com.unity.render-pipelines.core@')),
    'Samples~', 'Common', 'Materials', 'Shaders', 'SamplesCyclo.shadergraph'));
  const positionNode = o.cloneNode(cyclo, cyclo.find(x => x.m_Type.endsWith('.PositionNode')).m_ObjectId, -600, -420, { m_Space: 0 });
  const mul = o.cloneNode(templates, multiplyTemplate.m_ObjectId, -600, -160);
  const add = o.cloneNode(lt, lt.find(x => x.m_Type.endsWith('AddNode')).m_ObjectId, -320, -300);

  o.edge(normalNode.m_ObjectId, 0, mul.m_ObjectId, 0);
  o.edge(widthNode.m_ObjectId, 0, mul.m_ObjectId, 1);
  o.edge(positionNode.m_ObjectId, 0, add.m_ObjectId, 0);
  o.edge(mul.m_ObjectId, 2, add.m_ObjectId, 1);
  o.edge(add.m_ObjectId, 2, positionBlock.m_ObjectId, 0);

  write(path.join(outDir, 'SG_Outline.shadergraph'), objs);
}
console.log('Shader graphs written to', outDir);
