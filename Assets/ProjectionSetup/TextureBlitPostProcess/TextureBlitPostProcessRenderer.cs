using UnityEngine.Rendering.Universal;

[System.Serializable]
public class TextureBlitPostProcessRenderer : ScriptableRendererFeature
{
	TextureBlitPostProcessPass pass;

	public override void Create() {
		pass = new TextureBlitPostProcessPass();
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		renderer.EnqueuePass(pass);
	}
}
