using Android.Util;
using Android.Views;
using ZXing.Mobile;

namespace DroidUpc2;

public class LowestResolutionMatchingAspectRatioSelector(Activity m_activity)
{
    private CameraResolution? lastResolutionSet;

    public CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(
        List<CameraResolution> availableResolutions)
    {
        CameraResolution? result = null;

        WindowMetrics? metrics = m_activity.WindowManager?.CurrentWindowMetrics;
        bool fPortraitWindow = false;

//        DisplayMetrics displayMetrics = new DisplayMetrics();
        if (metrics != null)
        {
//            m_windowManager.DefaultDisplay.GetMetrics(displayMetrics);
//            SurfaceOrientation rotation = m_windowManager.DefaultDisplay.Rotation;
            SurfaceOrientation rotation = m_activity.WindowManager?.DefaultDisplay?.Rotation ?? SurfaceOrientation.Rotation0;

            //a tolerance of 0.1 should not be visible to the user
            double aspectTolerance = 0.1;
            //            bool fPortrait = rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180;
            //
            //            double displayOrientationHeight = fPortrait
            //                ? displayMetrics.HeightPixels
            //                : displayMetrics.WidthPixels;
            //            double displayOrientationWidth = fPortrait
            //                ? displayMetrics.WidthPixels
            //                : displayMetrics.HeightPixels;

            fPortraitWindow = rotation == SurfaceOrientation.Rotation0 || rotation == SurfaceOrientation.Rotation180;

            double displayOrientationHeight = metrics.Bounds.Height();
            double displayOrientationWidth = metrics.Bounds.Width();


            //calculating our targetRatio
            double targetRatio = displayOrientationHeight / displayOrientationWidth;
            double targetHeight = displayOrientationHeight;
            double minDiff = double.MaxValue;

            //camera API lists all available resolutions from highest to lowest, perfect for us
            //making use of this sorting, following code runs some comparisons to select the lowest
            //resolution that matches the screen aspect ratio and lies within tolerance
            //selecting the lowest makes Qr detection actual faster most of the time
            // (make sure we at least get 600 pixels on the width)
            foreach (var r in availableResolutions.Where(
                         r =>
                             (Math.Abs(((double)r.Width / r.Height) - targetRatio) < aspectTolerance) && r.Width > 600))
            {
                //slowly going down the list to the lowest matching solution with the correct aspect ratio
                if (Math.Abs(r.Height - targetHeight) < minDiff)
                    minDiff = Math.Abs(r.Height - targetHeight);
                result = r;
            }

            if (result == null)
            {
                CameraResolution? smallestDiff = availableResolutions.OrderBy(
                        s =>
                        {
                            bool fPortraitCamera = s.Width < s.Height;

                            double ratio = fPortraitCamera
                                ? (double)s.Height / s.Width
                                : (double)s.Width / s.Height;
                            return Math.Abs(ratio - targetRatio);
                        })
                   .FirstOrDefault();

                if (smallestDiff != null)
                {
                    result = new CameraResolution()
                          {
                              Width = smallestDiff.Width,
                              Height = smallestDiff.Height,
                          };

                }
            }
        }

        if (result == null)
            result = availableResolutions.Count > 0 ? availableResolutions[0] : null;

        if (result != null)
        {
            bool fPortraitCamera = result.Width < result.Height;
            lastResolutionSet =
                new CameraResolution()
                {
                    Width = fPortraitCamera == fPortraitWindow ? result.Width : result.Height,
                    Height = fPortraitCamera != fPortraitWindow ? result.Width : result.Height
                };
        }

        return result!;
    }

    /*----------------------------------------------------------------------------
        %%Function: GetUpdatedLayoutParametersIfNecessary
        %%Qualified: DroidUpc.Scanner.GetUpdatedLayoutParametersIfNecessary

        Recalculate the scanner box layout params for the aspect ratio of the
        camera, if needed
    ----------------------------------------------------------------------------*/
    public ViewGroup.LayoutParams? GetUpdatedLayoutParametersIfNecessary(ViewGroup.LayoutParams? current)
    {
        if (lastResolutionSet != null && current != null)
        {
            float ratio = (float)lastResolutionSet.Width / (float)lastResolutionSet.Height;
            float newWidth = (float)current.Height * ratio;

            return
                new LinearLayout.LayoutParams((int)newWidth, current.Height);
        }
        else
        {
            return null;
        }
    }
}
