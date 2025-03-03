﻿using CORERenderer.Loaders;
using CORERenderer.Main;
using static CORERenderer.OpenGL.Rendering;
using static CORERenderer.OpenGL.GL;
using COREMath;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW;
using SharpFont;

namespace CORERenderer.GUI
{
    public class Arrows
    {
        private uint VBO, VAO, EBO;

        private List<List<float>> vertices = new();
        private List<List<uint>> indices;

        private float maxScale = 0;

        public int pickedID;
        private int previousID;

        private Shader pickShader = GenericShaders.IDPicking;
        private Shader shader = GenericShaders.Arrow;

        private Vector3[] rgbs = new Vector3[9];

        private Model rotation;

        public bool wantsToMoveXAxis = false, wantsToMoveZAxis = false, wantsToMoveYAxis = false;
        public bool wantsToScaleXAxis = false, wantsToScaleYAxis = false, wantsToScaleZAxis = false;
        public bool wantsToRotateXAxis = false, wantsToRotateYAxis = false, wantsToRotateZAxis = false;
        public bool isBeingUsed = false;

        public static bool disableArrows = false;

        public Arrows()
        {
            rotation = new($"{Main.COREMain.BaseDirectory}\\OBJs\\triangle.stl");
            rotation.submodels[0].renderIDVersion = false;

            Readers.LoadOBJ($"{Main.COREMain.BaseDirectory}\\OBJs\\arrow.obj", out _, out List<List<Vertex>> lVertices, out indices, out _, out _, out _, out _);
            foreach (Vertex v in lVertices[0])
            {
                vertices.Add(new());
                vertices[0].Add(v.x); vertices[0].Add(v.y); vertices[0].Add(v.z);
                vertices[0].Add(v.uvX); vertices[0].Add(v.uvY);
                vertices[0].Add(v.normalX); vertices[0].Add(v.normalY); vertices[0].Add(v.normalZ);
            }

            GenerateFilledBuffer(out VBO, out VAO, vertices[0].ToArray());

            shader.Use();

            shader.ActivateAttributes();

            GenerateFilledBuffer(out EBO, indices[0].ToArray());

            for (int i = 0; i < 9; i++)
                rgbs[i] = Main.COREMain.GenerateIDColor(i);
                
            if (Main.COREMain.scenes[Main.COREMain.SelectedScene].currentObj == -1)
                return;

            maxScale = MathC.GetLengthOf(Main.COREMain.CurrentScene.camera.position - Main.COREMain.CurrentModel.Transform.translation) * 0.8f;
        }

        public void Render()
        {
            if (Main.COREMain.CurrentScene.currentObj == -1 || disableArrows)
                return;

            if (maxScale == 0)
                maxScale = (MathC.GetLengthOf(Main.COREMain.CurrentScene.camera.position - Main.COREMain.CurrentModel.Transform.translation)) / 2;

            shader.Use();


            //model matrix to place the arrows at the coordinates of the selected object, model * place of object * normalized size (to make the arrows always the same size)
            Matrix model = Matrix.IdentityMatrix * MathC.GetRotationMatrix(Main.COREMain.CurrentModel.Transform.rotation) * MathC.GetTranslationMatrix((new Vector4(Main.COREMain.CurrentModel.Transform.BoundingBox.center, 1) * Main.COREMain.CurrentModel.Transform.ModelMatrix).xyz) * MathC.GetScalingMatrix(MathC.GetLengthOf(Main.COREMain.CurrentScene.camera.position - Main.COREMain.CurrentModel.Transform.translation) * 0.2f);

            shader.SetVector3("color", 0, 1, 0);

            glBindVertexArray(VAO);

            for (int i = 0; i < 3; i++)
            {
                Matrix local = model;
                if (i == 0)
                    local *= MathC.GetRotationXMatrix(90);
                if (i == 1)
                {
                    shader.SetVector3("color", 1, 0, 0);
                    local *= MathC.GetRotationYMatrix(-90);
                }
                else if (i == 2)
                {
                    shader.SetVector3("color", 0, 0, 1);
                    local *= MathC.GetRotationYMatrix(180);
                }

                shader.SetMatrix("model", local);
                unsafe { glDrawElements(PrimitiveType.Triangles, indices[0].Count, GLType.UnsingedInt, (void*)0); }
            }

            float maxSize = (MathC.GetLengthOf(Main.COREMain.CurrentScene.camera.position - Main.COREMain.CurrentModel.Transform.translation) / maxScale);

            //draw the triangles that indicate the rotations
            glDisable(GL_CULL_FACE);

            //triangle 1
            shader.SetVector3("color", 1, 1, 0);
            Matrix triangleModel = model * MathC.GetTranslationMatrix(.1f, 0.35f, 0) * MathC.GetScalingMatrix(.1f);
            shader.SetMatrix("model", triangleModel);

            rotation.submodels[0].vbo.Draw(PrimitiveType.Triangles, 0, rotation.submodels[0].NumberOfVertices);

            //triangle 2
            shader.SetVector3("color", 0, 1, 1);
            triangleModel = model * MathC.GetTranslationMatrix(0, 0.35f, .15f) * MathC.GetScalingMatrix(.1f) * MathC.GetRotationYMatrix(-90);
            shader.SetMatrix("model", triangleModel);

            rotation.submodels[0].vbo.Draw(PrimitiveType.Triangles, 0, rotation.submodels[0].NumberOfVertices);

            //triangle 3
            shader.SetVector3("color", 1, 0, 1);
            triangleModel = model * MathC.GetTranslationMatrix(.1f, 0, .3f) * MathC.GetScalingMatrix(.1f) * MathC.GetRotationXMatrix(90);
            shader.SetMatrix("model", triangleModel);

            rotation.submodels[0].vbo.Draw(PrimitiveType.Triangles, 0, rotation.submodels[0].NumberOfVertices);

            glEnable(GL_CULL_FACE);

            //GenericShaders.Lighting.SetVector3("overrideColor", Vector3.Zero);

            RenderIDVersion();

            previousID = COREMain.selectedID;
        }

        private Shader alternatePickShader = GenericShaders.BonelessPickShader;

        private void RenderIDVersion()
        {
            COREMain.IDFramebuffer.Bind();

            glClear(GL_DEPTH_BUFFER_BIT);

            if (maxScale == 0)
                maxScale = (MathC.GetLengthOf(COREMain.CurrentScene.camera.position - COREMain.CurrentModel.Transform.translation)) / 2;

            Matrix model = Matrix.IdentityMatrix;

            //model matrix to place the arrows at the coordinates of the selected object, model * place of object * normalized size (to make the arrows always the same size)
            model *= MathC.GetRotationMatrix(COREMain.CurrentModel.Transform.rotation) * MathC.GetTranslationMatrix((new Vector4(COREMain.CurrentModel.Transform.BoundingBox.center, 1) * Main.COREMain.CurrentModel.Transform.ModelMatrix).xyz) * MathC.GetScalingMatrix(MathC.GetLengthOf(Main.COREMain.CurrentScene.camera.position - Main.COREMain.CurrentModel.Transform.translation) * 0.2f);

            glBindVertexArray(VAO);
            for (int i = 0; i < 3; i++)
            {
                alternatePickShader.Use();
                Matrix local = Matrix.IdentityMatrix * model;
                if (i == 0)
                {
                    alternatePickShader.SetVector3("color", rgbs[0]);
                    local *= MathC.GetRotationXMatrix(90);
                }
                if (i == 1)
                {
                    alternatePickShader.SetVector3("color", rgbs[1]);
                    local *= MathC.GetRotationYMatrix(-90);
                }
                else if (i == 2)
                {
                    alternatePickShader.SetVector3("color", rgbs[2]);
                    local *= MathC.GetRotationYMatrix(180);
                }

                alternatePickShader.SetMatrix("model", local);
                unsafe { glDrawElements(PrimitiveType.Triangles, indices[0].Count, GLType.UnsingedInt, (void*)0); }
            }
            float maxSize = (MathC.GetLengthOf(Main.COREMain.CurrentScene.camera.position - Main.COREMain.CurrentModel.Transform.translation) / maxScale);

            //draw the triangles that indicate the rotations
            glDisable(GL_CULL_FACE);
            //triangle 1
            pickShader.SetVector3("color", rgbs[6]);
            Matrix triangleModel = model * MathC.GetTranslationMatrix(.1f, 0.35f, 0) * MathC.GetScalingMatrix(.1f);
            pickShader.SetMatrix("model", triangleModel);

            rotation.submodels[0].vbo.Draw(PrimitiveType.Triangles, 0, rotation.submodels[0].NumberOfVertices);

            //triangle 2
            pickShader.SetVector3("color", rgbs[7]);
            triangleModel = model * MathC.GetTranslationMatrix(0, 0.35f, .15f) * MathC.GetScalingMatrix(.1f) * MathC.GetRotationYMatrix(-90);
            pickShader.SetMatrix("model", triangleModel);

            rotation.submodels[0].vbo.Draw(PrimitiveType.Triangles, 0, rotation.submodels[0].NumberOfVertices);

            //triangle 3
            pickShader.SetVector3("color", rgbs[8]);
            triangleModel = model * MathC.GetTranslationMatrix(.1f, 0, .3f) * MathC.GetScalingMatrix(.1f) * MathC.GetRotationXMatrix(90);
            pickShader.SetMatrix("model", triangleModel);

            rotation.submodels[0].vbo.Draw(PrimitiveType.Triangles, 0, rotation.submodels[0].NumberOfVertices);

            glEnable(GL_CULL_FACE);

            //GenericShaders.Lighting.SetVector3("overrideColor", Vector3.Zero);

            Main.COREMain.renderFramebuffer.Bind();
        }

        public void UpdateArrowsMovement()
        { //dont know if it can be coded better
            if (!wantsToScaleXAxis && !wantsToScaleYAxis && !wantsToScaleZAxis && !wantsToRotateXAxis && !wantsToRotateYAxis && !wantsToRotateZAxis)
            {
                if (Main.COREMain.selectedID == 0 && !wantsToMoveXAxis && !wantsToMoveZAxis)
                    wantsToMoveYAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToMoveYAxis = false;

                if (Main.COREMain.selectedID == 1 && !wantsToMoveYAxis && !wantsToMoveZAxis)
                    wantsToMoveXAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToMoveXAxis = false;

                if (Main.COREMain.selectedID == 2 && !wantsToMoveXAxis && !wantsToMoveYAxis)
                    wantsToMoveZAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToMoveZAxis = false;
            }
            
            if (!wantsToMoveXAxis && !wantsToMoveYAxis && !wantsToMoveZAxis && !wantsToRotateXAxis && !wantsToRotateYAxis && !wantsToRotateZAxis)
            {
                if (Main.COREMain.selectedID == 3 && !wantsToScaleXAxis && !wantsToScaleZAxis)
                    wantsToScaleYAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToScaleYAxis = false;

                if (Main.COREMain.selectedID == 4 && !wantsToScaleYAxis && !wantsToScaleZAxis)
                    wantsToScaleXAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToScaleXAxis = false;

                if (Main.COREMain.selectedID == 5 && !wantsToScaleXAxis && !wantsToScaleYAxis)
                    wantsToScaleZAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToScaleZAxis = false;
            }

            if (!wantsToMoveXAxis && !wantsToMoveYAxis && !wantsToMoveZAxis && !wantsToScaleXAxis && !wantsToScaleYAxis && !wantsToScaleZAxis)
            {
                if (Main.COREMain.selectedID == 6 && !wantsToRotateXAxis && !wantsToRotateYAxis)
                    wantsToRotateZAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToRotateZAxis = false;

                if (Main.COREMain.selectedID == 7 && !wantsToRotateYAxis && !wantsToRotateZAxis)
                    wantsToRotateXAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToRotateXAxis = false;

                if (Main.COREMain.selectedID == 8 && !wantsToRotateXAxis && !wantsToRotateZAxis)
                    wantsToRotateYAxis = true;

                else if (Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) != InputState.Press)
                    wantsToRotateYAxis = false;
            }

            isBeingUsed = wantsToScaleXAxis || wantsToScaleYAxis || wantsToScaleZAxis || wantsToRotateXAxis || wantsToRotateYAxis || wantsToRotateZAxis || wantsToMoveXAxis || wantsToMoveYAxis || wantsToMoveZAxis;
        }
    }
}