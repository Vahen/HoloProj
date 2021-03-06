﻿using HoloToolkit.Unity;
using System.Collections;
using System.IO;
using UnityEngine;

using UnityGLTF;

public class GLTFComponentPerso : Singleton<GLTFComponentPerso> {

    public bool Multithreaded = false;
    public bool UseStream = true;

    public int MaximumLod = 300;

    public Shader GLTFStandard;
    public Shader GLTFStandardSpecular;
    public Shader GLTFConstant;

    public bool addColliders = true;

    public Stream GLTFStream = null;

    public bool IsLoaded { get; set; }

    /// <summary>
    /// Initialise les shaders au lancement
    /// </summary>
    private void Awake() {
        GLTFStandard = Shader.Find("GLTF/GLTFStandard");
        GLTFStandardSpecular = Shader.Find("GLTF/GLTFStandard");
        GLTFConstant = Shader.Find("GLTF/GLTFConstant");
    }

    /// <summary>
    /// Permet de charger le fichier GLTF voulue
    /// </summary>
    /// <param name="filePath"> Chemin fichier à charger</param>
    /// <param name="parent"> Object parent auquel rattaché l'objet gltf chargé</param>
    /// <returns></returns>
    public IEnumerator CreateComponentFromFile(string filePath, GameObject parent) {
        GLTFSceneImporter loader = null;
        string Url = filePath;
        if (UseStream) {
            string fullPath = "";

            if (GLTFStream == null) {
                fullPath = Url; //Path.Combine(Application.streamingAssetsPath, Url);
                Debug.Log("FullPath : " + fullPath);
                GLTFStream = File.OpenRead(fullPath);
            }
            loader = new GLTFSceneImporter(
                fullPath,
                GLTFStream,
                parent.transform,
                addColliders
                );
        }
        else {
            loader = new GLTFSceneImporter(
                Url,
                parent.transform,
                addColliders
                );
        }
        loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandard);
        loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.KHR_materials_pbrSpecularGlossiness, GLTFStandardSpecular);
        loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.CommonConstant, GLTFConstant);
        loader.MaximumLod = MaximumLod;
        yield return loader.Load(-1, Multithreaded);
        if (GLTFStream != null) {
#if WINDOWS_UWP
            GLTFStream.Dispose();
#else
            GLTFStream.Close();
#endif

            GLTFStream = null;
        }

        IsLoaded = true;
    }


    public IEnumerator WaitForModelLoad() {
        while (!IsLoaded) {
            yield return null;
        }
    }
}
