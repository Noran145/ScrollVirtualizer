using UnityEngine;

namespace NoranDev.ScrollVirtualizer
{
    /// <summary>
    /// Easing function utility class
    /// </summary>
    internal static class EasingFunction
    {
        /// <summary>
        /// Interpolate with easing applied
        /// </summary>
        /// <param name="t">Normalized time</param>
        /// <param name="ease">Easing type</param>
        /// <returns>Value after easing applied</returns>
        public static float Interpolate(float t, Ease ease)
        {
            switch (ease)
            {
                case Ease.Linear:
                    return t;

                case Ease.InQuad:
                    return InQuad(t);
                case Ease.OutQuad:
                    return OutQuad(t);
                case Ease.InOutQuad:
                    return InOutQuad(t);

                case Ease.InCubic:
                    return InCubic(t);
                case Ease.OutCubic:
                    return OutCubic(t);
                case Ease.InOutCubic:
                    return InOutCubic(t);

                case Ease.InQuart:
                    return InQuart(t);
                case Ease.OutQuart:
                    return OutQuart(t);
                case Ease.InOutQuart:
                    return InOutQuart(t);

                case Ease.InQuint:
                    return InQuint(t);
                case Ease.OutQuint:
                    return OutQuint(t);
                case Ease.InOutQuint:
                    return InOutQuint(t);

                case Ease.InSine:
                    return InSine(t);
                case Ease.OutSine:
                    return OutSine(t);
                case Ease.InOutSine:
                    return InOutSine(t);

                case Ease.InExpo:
                    return InExpo(t);
                case Ease.OutExpo:
                    return OutExpo(t);
                case Ease.InOutExpo:
                    return InOutExpo(t);

                case Ease.InCirc:
                    return InCirc(t);
                case Ease.OutCirc:
                    return OutCirc(t);
                case Ease.InOutCirc:
                    return InOutCirc(t);

                case Ease.InBack:
                    return InBack(t);
                case Ease.OutBack:
                    return OutBack(t);
                case Ease.InOutBack:
                    return InOutBack(t);

                case Ease.InElastic:
                    return InElastic(t);
                case Ease.OutElastic:
                    return OutElastic(t);
                case Ease.InOutElastic:
                    return InOutElastic(t);

                case Ease.InBounce:
                    return InBounce(t);
                case Ease.OutBounce:
                    return OutBounce(t);
                case Ease.InOutBounce:
                    return InOutBounce(t);

                default:
                    return t;
            }
        }

        private static float InQuad(float t) => t * t;
        private static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        private static float InCubic(float t) => t * t * t;
        private static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        private static float InQuart(float t) => t * t * t * t;
        private static float OutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
        private static float InOutQuart(float t) => t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;

        private static float InQuint(float t) => t * t * t * t * t;
        private static float OutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);
        private static float InOutQuint(float t) => t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;

        private static float InSine(float t) => 1f - Mathf.Cos(t * Mathf.PI / 2f);
        private static float OutSine(float t) => Mathf.Sin(t * Mathf.PI / 2f);
        private static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        private static float InExpo(float t) => t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
        private static float OutExpo(float t) => t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        private static float InOutExpo(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
        }

        private static float InCirc(float t) => 1f - Mathf.Sqrt(1f - t * t);
        private static float OutCirc(float t) => Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
        private static float InOutCirc(float t) => t < 0.5f
            ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f
            : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;

        private const float BackC1 = 1.70158f;
        private const float BackC2 = BackC1 * 1.525f;
        private const float BackC3 = BackC1 + 1f;

        private static float InBack(float t) => BackC3 * t * t * t - BackC1 * t * t;
        private static float OutBack(float t) => 1f + BackC3 * Mathf.Pow(t - 1f, 3f) + BackC1 * Mathf.Pow(t - 1f, 2f);
        private static float InOutBack(float t) => t < 0.5f
            ? (Mathf.Pow(2f * t, 2f) * ((BackC2 + 1f) * 2f * t - BackC2)) / 2f
            : (Mathf.Pow(2f * t - 2f, 2f) * ((BackC2 + 1f) * (t * 2f - 2f) + BackC2) + 2f) / 2f;

        private const float ElasticC4 = (2f * Mathf.PI) / 3f;
        private const float ElasticC5 = (2f * Mathf.PI) / 4.5f;

        private static float InElastic(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * ElasticC4);
        }

        private static float OutElastic(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * ElasticC4) + 1f;
        }

        private static float InOutElastic(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return t < 0.5f
                ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * ElasticC5)) / 2f
                : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * ElasticC5)) / 2f + 1f;
        }

        private static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }

        private static float InBounce(float t) => 1f - OutBounce(1f - t);
        private static float InOutBounce(float t) => t < 0.5f
            ? (1f - OutBounce(1f - 2f * t)) / 2f
            : (1f + OutBounce(2f * t - 1f)) / 2f;
    }
}
