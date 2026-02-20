using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class adjustment_offset : MonoBehaviour
{
    // 調整するボーンのTransformをInspectorから設定します
    public Transform Bone_mouth_01;
    public Transform Bone_mouth_02;
    public Transform Bone_eye_L_01;
    public Transform Bone_eye_R_01;
    public Transform Bone_eye_L_02;
    public Transform Bone_eye_R_02;
    public Transform Bone_eye_L_002;
    public Transform Bone_eye_R_002;
    public Transform eye_R_03;
    public Transform eye_L_03;
    public Transform Face_01;
    public Transform Face_02;


    // 調整する位置オフセット
    public Vector3 mouth01Offset;
    public Vector3 mouth02Offset;
    public Vector3 eyeL01Offset;
    public Vector3 eyeR01Offset;
    public Vector3 eyeL02Offset;
    public Vector3 eyeR02Offset;
    public Vector3 face01Offset;
    public Vector3 face02Offset;
    public Vector3 eyeR03Offset;
    public Vector3 eyeL03Offset;
    public Vector3 eyeL002Offset;
    public Vector3 eyeR002Offset;

    // アニメーション再生中の位置調整
    void LateUpdate()
    {
        if (Bone_mouth_01 != null)
        {
            Bone_mouth_01.localPosition += mouth01Offset;
        }
        if (Bone_mouth_02 != null)
        {
            Bone_mouth_02.localPosition += mouth02Offset;
        }
        if (Bone_eye_L_01 != null)
        {
            Bone_eye_L_01.localPosition += eyeL01Offset;
        }
        if (Bone_eye_R_01 != null)
        {
            Bone_eye_R_01.localPosition += eyeR01Offset;
        }
        if (Bone_eye_L_02 != null)
        {
            Bone_eye_L_02.localPosition += eyeL02Offset;
        }
        if (Bone_eye_R_02 != null)
        {
            Bone_eye_R_02.localPosition += eyeR02Offset;
        }
        if (Face_01 != null)
        {
            Face_01.localPosition += face01Offset;
        }
        if (Face_02 != null)
        {
            Face_02.localPosition += face02Offset;
        }
        if (eye_R_03 != null)
        {
            eye_R_03.localPosition += eyeR03Offset;
        }
        if (eye_L_03 != null)
        {
            eye_L_03.localPosition += eyeL03Offset;
        }
        if (Bone_eye_L_002 != null)
        {
            Bone_eye_L_002.localPosition += eyeL002Offset;
        }
        if (Bone_eye_R_002 != null)
        {
            Bone_eye_R_002.localPosition += eyeR002Offset;
        }
    }
}
