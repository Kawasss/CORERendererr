﻿using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using Console = CORERenderer.GUI.Console;

namespace CORERenderer.Loaders
{
    public partial class Model : Readers
    {
        public static Model Cube { get => new($"{COREMain.BaseDirectory}\\OBJs\\cube.obj"); }
        public static Model Cylinder { get => new($"{COREMain.BaseDirectory}\\OBJs\\cylinder.obj"); }
        public static Model Plane { get => new ($"{COREMain.BaseDirectory}\\OBJs\\plane.obj"); }
        public static Model Sphere { get => new($"{COREMain.BaseDirectory}\\OBJs\\sphere.obj"); }

        public static int totalSSBOSizeUsed = 0;

        #region Properties
        /// <summary>
        /// Gives the vertices of the submodels, each submodel is a new list. Translations are not applied to this.
        /// </summary>
        public List<List<Vertex>> Vertices { get { List<List<Vertex>> value = new(); foreach (Submodel s in submodels) value.Add(s.Vertices); return value; } } //adds the vertices from the submodels into one list

        public List<PBRMaterial> Materials { get { List<PBRMaterial> value = new(); foreach (Submodel s in submodels) value.Add(s.material); return value; } } //adds the materials from the submodels into one list

        /// <summary>
        /// Gives the current translations of all of the submodels
        /// </summary>
        public List<Vector3> Offsets { get { List<Vector3> value = new(); value.Add(transform.translation); foreach (Submodel s in submodels) value.Add(s.translation); return value; } } //adds the materials from the submodels into one list

        public Submodel CurrentSubmodel { get => submodels[selectedSubmodel]; }

        public string AmountOfVertices { get => totalAmountOfVertices / 1000 >= 1 ? $"{MathF.Round(totalAmountOfVertices / 1000):N0}k" : $"{totalAmountOfVertices}"; }

        public string Name { get => name; set => name = value.Length > 10 ? value[..10] : value; }
        public string FullPath { get => path; }

        public bool CanBeCulled { get => !transform.BoundingBox.IsInFrustum(Rendering.Camera.Frustum, transform); }
        #endregion

        public List<Submodel> submodels = new();

        public Shader shader = GenericShaders.Lighting;

        public ModelType type;
        public Error error = Error.None;

        private string name = "PLACEHOLDER";
        private string path = COREMain.BaseDirectory;
        
        public bool highlighted = false, renderLines = false, renderNormals = false, terminate = false;

        public string mtllib;

        public int ID;

        private int totalAmountOfVertices = 0;

        public int selectedSubmodel = 0;

        private Transform transform = new();
        public Transform Transform { get { return transform; } set { transform = value; } } //!!REMOVE SET WHEN DONE DEBUGGING

        public Model(string path)
        {
            ID = COREMain.NewAvaibleID;
            type = COREMain.GetModelType(path);

            if (type == ModelType.ObjFile)
                GenerateObj(path);

            else if (type == ModelType.STLFile)
                GenerateStl(path);

            else if (type == ModelType.FBXFile)
                GenerateFbx(path);

            else if (type == ModelType.CPBRFile)
                GeneratePBR(path);

            else if (type == ModelType.JPGImage || type == ModelType.PNGImage)
                GenerateImage(path);
        }
        
        public Model(string path, List<List<Vertex>> vertices, List<List<uint>> indices, List<PBRMaterial> materials, List<Vector3> offsets, Vector3 center, Vector3 extents)
        {
            ID = COREMain.NewAvaibleID;
            type = COREMain.GetModelType(path);

            Name = Path.GetFileName(path)[..^4];

            submodels = new();
            this.transform = new(offsets[0], Vector3.Zero, new(1, 1, 1), extents, center);
            int amountOfFailures = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                try
                {
                    submodels.Add(new(this.name, vertices[i], indices[i], offsets[i] - this.Transform.translation, this, materials[i]));
                    totalAmountOfVertices += submodels[^1].NumberOfVertices;
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteError($"Couldn't create submodel {i} out of {vertices.Count - 1} for model {COREMain.CurrentScene.models.Count} \"{Name}\"");
                    amountOfFailures++;
                    continue;
                }
            }
            if (amountOfFailures >= vertices.Count - 1)
                terminate = true;
            if (!terminate)
            {
                submodels[0].highlighted = true;
                selectedSubmodel = 0;
            }
        }

        public Model() { ID = Main.COREMain.NewAvaibleID; }

        private void GenerateFbx(string path)
        {
            double startedReading = Glfw.Time;
            Error loaded = LoadOBJ(path, out name, out List<List<Vertex>> lVertices, out List<PBRMaterial> materials, out Vector3 center, out Vector3 extents, out Vector3 offset);
            double readFBXFile = Glfw.Time - startedReading;

            CheckError(loaded);

            transform = new(offset, Vector3.Zero, new(1, 1, 1), extents, center);
            for (int i = 0; i < lVertices.Count; i++)
            {
                submodels.Add(new(name, lVertices[i], materials[i], this));
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }
            Console.WriteDebug($"Read FBX file in {Math.Round(readFBXFile, 2)} seconds");

            submodels[0].highlighted = true;
            selectedSubmodel = 0;
        }

        private void GeneratePBR(string path)
        {
            double startedReading = Glfw.Time;
            PBRMaterial material = LoadCPBR(path);
            double readFile = Glfw.Time - startedReading;

            Error loaded = LoadOBJ($"{COREMain.BaseDirectory}\\OBJs\\sphere.obj", out name, out List<List<Vertex>> lVertices, out _, out Vector3 center, out Vector3 extents, out Vector3 offset);
            CheckError(loaded);

            transform = new(offset, Vector3.Zero, new(1, 1, 1), extents, center);

            Console.WriteDebug($"Read CPBR file in {Math.Round(readFile, 2)} seconds");

            submodels.Add(new(Path.GetFileNameWithoutExtension(path), lVertices[0], material, this));

            submodels[0].highlighted = true;
            selectedSubmodel = 0;
        }

        private void GenerateObj(string path)
        {
            double startedReading = Glfw.Time;
            Error loaded = LoadOBJ(path, out name, out List<List<Vertex>> lVertices, out List<PBRMaterial> materials, out Vector3 center, out Vector3 extents, out Vector3 offset);
            double readOBJFile = Glfw.Time - startedReading;

            CheckError(loaded);

            transform = new(offset, Vector3.Zero, new(1, 1, 1), extents, center);
            for (int i = 0; i < lVertices.Count; i++)
            {
                submodels.Add(new(name, lVertices[i], materials[i], this));
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }

            submodels[0].highlighted = true;
            selectedSubmodel = 0;

            Console.WriteDebug($"Read .obj file in {Math.Round(readOBJFile, 2)} seconds");
            Console.WriteDebug($"Amount of vertices: {AmountOfVertices}");
        }


        private void GenerateStl(string path)
        {
            double startedReading = Glfw.Time;
            Error loaded = LoadSTL(path, out name, out List<float> localVertices, out Vector3 offset);
            double readSTLFile = Glfw.Time - startedReading;

            Vector3 min = Vector3.Zero, max = Vector3.Zero;
            for (int i = 0; i < localVertices.Count; i += 8)
            {
                max.x = localVertices[i] > max.x ? localVertices[i] : max.x;
                max.y = localVertices[i + 1] > max.y ? localVertices[i + 1] : max.y;
                max.z = localVertices[i + 2] > max.z ? localVertices[i + 2] : max.z;

                min.x = localVertices[i] < min.x ? localVertices[i] : min.x;
                min.y = localVertices[i + 1] < min.y ? localVertices[i + 1] : min.y;
                min.z = localVertices[i + 2] < min.z ? localVertices[i + 2] : min.z;
            }
            Vector3 center = (min + max) * 0.5f;
            Vector3 extents = max - center;
            transform = new(offset, Vector3.Zero, new(1, 1, 1), extents, center);

            CheckError(loaded);

            submodels.Add(new(Path.GetFileNameWithoutExtension(path), Vertex.GetVertices(localVertices), offset, new(1, 1, 1), this));

            Console.WriteDebug($"Read .stl file in {Math.Round(readSTLFile, 2)} seconds");
            Console.WriteDebug($"Amount of vertices: {submodels[^1].NumberOfVertices}");
        }

        private void GenerateImage(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            PBRMaterial material = new() { albedo = FindTexture(path) };
            float width = material.albedo.width * 0.002f;
            float height = material.albedo.height * 0.002f;

            float[] iVertices = new float[48]
            {
                -width / 2, 0.1f, -height / 2,    0, 1,   0, 1, 0,
                -width / 2, 0.1f,  height / 2,    0, 0,   0, 1, 0,
                width / 2,  0.1f,  height / 2,    1, 0,   0, 1, 0,

                -width / 2, 0.1f, -height / 2,    0, 1,   0, 1, 0,
                width / 2,  0.1f,  height / 2,    1, 0,   0, 1, 0,
                width / 2,  0.1f, -height / 2,    1, 1,   0, 1, 0
            };
            Vector3 max = new(width / 2, 0.1f, height / 2);
            Vector3 min = new(-width / 2, 0.1f, -height / 2);
            Vector3 center = (min + max) * 0.5f;
            Vector3 extents = max - center;
            this.transform = new(Vector3.Zero, Vector3.Zero, new(1, 1, 1), extents, center);
            submodels.Add(new(Name, Vertex.GetVertices(iVertices.ToList()), Vector3.Zero, this, material));
            submodels[^1].cullFaces = true;
        }

        private void CheckError(Error loaded)
        {
            if (loaded == Error.None)
                return;
            
            this.error = loaded;
            terminate = true;
        }

        private void SortSubmodelsByDepth()
        {
            Submodel[] submodelsInCorrectOrder = new Submodel[submodels.Count];
            List<float> distances = new();
            Dictionary<float, Submodel> distanceSubmodelTable = new();

            foreach (Submodel submodel in submodels)
            {
                float distance = submodel.translation.Length;
                while (distanceSubmodelTable.ContainsKey(distance))
                    distance += 0.01f;
                distances.Add(distance);
                distanceSubmodelTable.Add(distance, submodel);
            }

            float[] distancesArray = distances.ToArray();
            Array.Sort(distancesArray);
            for (int i = 0; i < distancesArray.Length; i++)
                submodelsInCorrectOrder[i] = distanceSubmodelTable[distancesArray[i]];
            submodels = submodelsInCorrectOrder.ToList();
        }

        public void Reset()
        {
            transform = new();
        }

        /// <summary>
        /// The loads the file at the path of the model at the place of the model
        /// </summary>
        public void Reload() => ReloadModel(path, this);

        private static void ReloadModel(string path, Model model)
        {
            model.Dispose();
            model = new(path);
            model.Reset();
        }

        public void Dispose()
        {
            if (Main.COREMain.CurrentScene.currentObj == Main.COREMain.CurrentScene.models.IndexOf(this))
                Main.COREMain.CurrentScene.currentObj = -1;
            foreach (Submodel submodel in submodels)
                submodel.Dispose();
            terminate = true;
        }
    }
}