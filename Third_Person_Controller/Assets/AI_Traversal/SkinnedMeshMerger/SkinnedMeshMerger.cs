using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkinnedMeshMerger : MonoBehaviour
{
    [Header("The object with the rig")]
    public GameObject CorrectModel;
    [Header("The object with the meshes that need to be transferred")]
    public GameObject ModelToAdapt;

    SkinnedMeshRenderer[] skinnedMeshesToFix;

    public void MergeSkinnedMeshes()
    {        
        skinnedMeshesToFix = ModelToAdapt.GetComponentsInChildren<SkinnedMeshRenderer>();
        PrefabUtility.UnpackPrefabInstance(ModelToAdapt, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);


        for (int i = 0; i < skinnedMeshesToFix.Length; i++)
        {
            Merge(skinnedMeshesToFix[i] , CorrectModel);
            skinnedMeshesToFix[i].transform.parent = CorrectModel.transform;
        }      
    }

    void Merge(SkinnedMeshRenderer targetSkin , GameObject correctRig)
    {
        string rootName = "";
        if (targetSkin.rootBone != null) rootName = targetSkin.rootBone.name;
        Transform newRoot = null;

        // Reassign new bones
        Transform[] newBones = new Transform[targetSkin.bones.Length];
        Transform[] existingBones = correctRig.GetComponentsInChildren<Transform>(true);
        int missingBones = 0;

        for (int i = 0; i < targetSkin.bones.Length; i++)
        {
            if (targetSkin.bones[i] == null)
            {                
                missingBones++;
                continue;
            }

            string boneName = targetSkin.bones[i].name;

            bool found = false;

            foreach (var newBone in existingBones)
            {
                if (newBone.name == rootName) newRoot = newBone;
                if (newBone.name == boneName)
                {
                    newBones[i] = newBone;
                    found = true;
                }
            }

            if (!found) missingBones++;          
        }

        targetSkin.bones = newBones;        

        if (newRoot != null) targetSkin.rootBone = newRoot;       
    }
}
