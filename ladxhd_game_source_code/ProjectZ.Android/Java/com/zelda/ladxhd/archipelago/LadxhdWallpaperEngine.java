package com.zelda.ladxhd.archipelago;

public final class LadxhdWallpaperEngine
    extends android.service.wallpaper.WallpaperService.Engine
    implements mono.android.IGCUserPeer
{
    public static final String __md_methods;
    static {
        __md_methods =
            "n_onVisibilityChanged:(Z)V:GetOnVisibilityChanged_ZHandler\n" +
            "n_onSurfaceCreated:(Landroid/view/SurfaceHolder;)V:GetOnSurfaceCreated_Landroid_view_SurfaceHolder_Handler\n" +
            "n_onSurfaceChanged:(Landroid/view/SurfaceHolder;III)V:GetOnSurfaceChanged_Landroid_view_SurfaceHolder_IIIHandler\n" +
            "n_onSurfaceDestroyed:(Landroid/view/SurfaceHolder;)V:GetOnSurfaceDestroyed_Landroid_view_SurfaceHolder_Handler\n" +
            "n_onOffsetsChanged:(FFFFII)V:GetOnOffsetsChanged_FFFFIIHandler\n" +
            "n_onTouchEvent:(Landroid/view/MotionEvent;)V:GetOnTouchEvent_Landroid_view_MotionEvent_Handler\n" +
            "n_onDestroy:()V:GetOnDestroyHandler\n";
        mono.android.Runtime.register(
            "ProjectZ.Android.LadxhdWallpaperService+LadxhdWallpaperEngine, ProjectZ.Android",
            LadxhdWallpaperEngine.class,
            __md_methods);
    }

    public LadxhdWallpaperEngine(android.service.wallpaper.WallpaperService owner)
    {
        owner.super();
        activate(owner);
    }

    public LadxhdWallpaperEngine(LadxhdWallpaperService owner)
    {
        owner.super();
        activate(owner);
    }

    private void activate(android.service.wallpaper.WallpaperService owner)
    {
        if (getClass() == LadxhdWallpaperEngine.class) {
            mono.android.TypeManager.Activate(
                "ProjectZ.Android.LadxhdWallpaperService+LadxhdWallpaperEngine, ProjectZ.Android",
                "Android.Service.Wallpaper.WallpaperService, Mono.Android",
                this,
                new java.lang.Object[] { owner });
        }
    }

    @Override public void onVisibilityChanged(boolean visible) { n_onVisibilityChanged(visible); }
    private native void n_onVisibilityChanged(boolean visible);

    @Override public void onSurfaceCreated(android.view.SurfaceHolder holder) { n_onSurfaceCreated(holder); }
    private native void n_onSurfaceCreated(android.view.SurfaceHolder holder);

    @Override public void onSurfaceChanged(android.view.SurfaceHolder holder, int format, int width, int height) {
        n_onSurfaceChanged(holder, format, width, height);
    }
    private native void n_onSurfaceChanged(android.view.SurfaceHolder holder, int format, int width, int height);

    @Override public void onSurfaceDestroyed(android.view.SurfaceHolder holder) { n_onSurfaceDestroyed(holder); }
    private native void n_onSurfaceDestroyed(android.view.SurfaceHolder holder);

    @Override public void onOffsetsChanged(float xOffset, float yOffset, float xStep, float yStep,
                                           int xPixels, int yPixels) {
        n_onOffsetsChanged(xOffset, yOffset, xStep, yStep, xPixels, yPixels);
    }
    private native void n_onOffsetsChanged(float xOffset, float yOffset, float xStep, float yStep,
                                           int xPixels, int yPixels);

    @Override public void onTouchEvent(android.view.MotionEvent event) { n_onTouchEvent(event); }
    private native void n_onTouchEvent(android.view.MotionEvent event);

    @Override public void onDestroy() { n_onDestroy(); }
    private native void n_onDestroy();

    private java.util.ArrayList refList;
    public void monodroidAddReference(java.lang.Object value) {
        if (refList == null) refList = new java.util.ArrayList();
        refList.add(value);
    }
    public void monodroidClearReferences() {
        if (refList != null) refList.clear();
    }
}
