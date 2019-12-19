using UnityEngine;
using UnityEditor;
using System;

public class CustomInspector : ShaderGUI
{
    enum ブレンドモード // 変更可能なブレンドモードの宣言
    {
        不透明,
        カットオフ,
        半透明,
    }

    // インスペクタの描画
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        ブレンドモード mode = DrawBlendMode(materialEditor, properties);// ブレンドモードはいろいろするので別関数

        // カットオフの時だけ、カットオフの閾値調整を表示する
        if (mode == ブレンドモード.カットオフ)
        {
            var cutoff = FindProperty("_Cutoff", properties);
            materialEditor.ShaderProperty(cutoff, cutoff.displayName);
        }

        var mainTex = FindProperty("_MainTex", properties);
        materialEditor.ShaderProperty(mainTex, mainTex.displayName);// テクスチャの設定

        var Color = FindProperty("_Color", properties);
        materialEditor.ShaderProperty(Color, Color.displayName);// 色の設定

        materialEditor.RenderQueueField();// 描画順を決めるキューの設定
    }

    ブレンドモード DrawBlendMode(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // シェーダから値を取得
        var blendMode = FindProperty("_BlendMode", properties);
        var mode = (ブレンドモード)blendMode.floatValue;

        using (var scope = new EditorGUI.ChangeCheckScope())// GUI要素の変更をとらえる
        {
            // "Blend Mode" をエディタに表示し、値が変更されたらその結果を返り値に受け取る
            mode = (ブレンドモード)EditorGUILayout.Popup("Blend Mode", (int)mode, Enum.GetNames(typeof(ブレンドモード)));

            if (scope.changed)// 値が変化してたら
            {
                blendMode.floatValue = (float)mode;// エディタの値をシェーダの変数に設定
                foreach (UnityEngine.Object マテリアル in blendMode.targets)// blendMode を持つマテリアルを全修正
                {
                    ApplyBlendMode(マテリアル as Material, mode);// ブレンドモードの設定を反映
                }
            }
        }

        return mode;
    }

    static void ApplyBlendMode(Material material, ブレンドモード mode)
    {
        switch (mode)
        {
            case ブレンドモード.不透明:
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");// #ifdef _ALPHATEST_ON の切り替え
                break;

            case ブレンドモード.カットオフ:
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");// αテストする
                break;

            case ブレンドモード.半透明:
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);// 内挿
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);// 深度書き込みしない
                material.DisableKeyword("_ALPHATEST_ON");
                break;

            default:
                throw new ArgumentOutOfRangeException("blendMode", mode, null);
        }
    }

    // MaterialのShader切り替え時にBlend指定が勝手に変更されてしまうので再設定する
    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        ApplyBlendMode(material, (ブレンドモード)material.GetFloat("_BlendMode"));
    }
}
