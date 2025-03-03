﻿using CORERenderer.Main;
using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using COREMath;
using CORERenderer.textures;
using System.Diagnostics;

namespace CORERenderer.GUI
{
    public partial class Div
    {
        private Shader shader = GenericShaders.Image2D;

        public void RenderModelInformation()
        {
            if (Main.COREMain.GetCurrentObjFromScene == -1)
                return;

            int totalOffset = 0;
            Model model = Main.COREMain.CurrentModel;

            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write($"Translation: {Math.Round(model.Transform.translation.x, 2)} {Math.Round(model.Transform.translation.y, 2)} {Math.Round(model.Transform.translation.z, 2)}", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write($"Scaling:     {Math.Round(model.Transform.scale.x, 2)} {Math.Round(model.Transform.scale.y, 2)} {Math.Round(model.Transform.scale.z, 2)}", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write($"Rotation:    {Math.Round(model.Transform.rotation.x, 2)} {Math.Round(model.Transform.rotation.y, 2)} {Math.Round(model.Transform.rotation.z, 2)}", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            //shitty
            Texture textureToDraw = model.submodels[0].material.albedo;
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Albedo:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth  / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = model.submodels[0].material.roughness;
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Roughness:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = model.submodels[0].material.normal;
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Normal:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = model.submodels[0].material.metallic;
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("metallic:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = model.submodels[0].material.AO;
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("AO:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = model.submodels[0].material.height;
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("height:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
        }

        private int previousAmount = 0;
        public void RenderModelList(List<Model> models)
        {
            if (previousAmount == models.Count)
                return;

            for (int i = 0; i < models.Count; i++)
            {
                float offset = this.Height - Main.COREMain.debugText.characterHeight * 0.8f * ((i + 1) * 2);

                if (offset <= 0) //return when the list goes outside the bounds of the div
                    return;

                //if the model is selected it gets a different color to reflect that
                Vector3 color = models[i].highlighted ? new(1, 0, 1) : new(1, 1, 1);
                this.Write($"[{models[i].type}] {models[i].Name}", (int)(this.Width * 0.03f), (int)offset, 0.7f, color);
                string plural = models[i].submodels.Count == 1 ? "" : "s";
                this.Write($"    {models[i].submodels.Count} submodel{plural}", (int)(this.Width * 0.03f), (int)(offset - Main.COREMain.debugText.characterHeight * 0.8f), 0.7f, color);
            }
        }
    }
}