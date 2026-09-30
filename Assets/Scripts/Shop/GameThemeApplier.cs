using UnityEngine;

namespace Shop
{
    public sealed class GameThemeApplier : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer ballRenderer;
        [SerializeField] private SpriteRenderer ringRenderer;
        [SerializeField] private ParticleSystem trailParticles;

        private void Awake()
        {
            Color ballColor = ShopCatalog.GetEquippedColor(ShopItemKind.BallTheme, ballRenderer.color);
            ballRenderer.color = ballColor;
            ParticleSystem.MainModule main = trailParticles.main;
            main.startColor = ballColor;

            ringRenderer.color = ShopCatalog.GetEquippedColor(ShopItemKind.WorldTheme, ringRenderer.color);
        }
    }
}
