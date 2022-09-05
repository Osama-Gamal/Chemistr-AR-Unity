************************************************
*               LIQUID VOLUME PRO              *
*            By Ramiro Oliva (Kronnect)        *   
*                  README FILE                 *
************************************************


How to use this asset
---------------------

Thanks for purchasing LIQUID VOLUME PRO!

Important: please read the brief quick start guide located in LiquidVolumePro/Documentation.


Support
-------

* Email support: contact@kronnect.com
* Website-Forum Support: http://kronnect.com
* Twitter: @Kronnect


Other Cool Assets!
------------------

Check our other assets on the Asset Store publisher page:
https://www.assetstore.unity3d.com/en/#!/search/page=1/sortby=popularity/query=publisher:15018



Version history
---------------

Version 3.0.4
- Memory and loading optimizations when using many instances of same flask
- [Fix] Fixed an issue with Unity 2021.2 that affects shader variants in a build

Version 3.0.3
- Memory optimizations for bubbles option

Version 3.0.2
- [Fix] Fixed miscible amount calculation bug

Version 3.0.1
- [Fix] Transparency for layers in a multiple detail level is now handled as in default style to ensure color matching

Version 3.0
- Added 30 ready-to-use chemistry flasks optimized for Liquid Volume Pro

Version 2.1
- Added "Smoke Height Reduction" parameter
- [Fix] Fixed build issue on Oculus Quest

Version 2.0.1
- [Fix] Additional fixes for orthographic camera

Version 2.0
- Material setters performance improveemnts
- [Fix] Fixed VR issue on Oculus Quest 2 with MultiView rendering mode

Version 1.9.1
- [Fix] Fixed depth texture shader compatibility issue with VR on certain platforms

Version 1.9
- Added support for orthographic cameras

Version 1.8
- Improved performance of "Close Mesh" tool

Version 1.7
- Added "Flask Color"
- Change: "Flask Tint" is now "Emission". Use Flask Color to add custom color to flask with opacity.

Version 1.6.3
- Change to bubble scale param handling
- Internal fixes and improvements related to VR
- [Fix] Setting bubble amount to 0 doesn't remove all bubbles
- [Fix] Fixed miscible layers issue

Version 1.6.2
- [Fix] Fixed "Multiple No Flask" style issue when static batching is enabled

Version 1.6.1
- Improved volume computation for maintaining surface position when flask is rotated (rotationLevelBias option)
- [Fix] Fixed parent aware depth issue
- [Fix] Fixed preview of depth / parent aware textures in inspector when floating point buffers are enabled

Version 1.6
- Added "Back Depth Bias" parameter to limit depth in irregular topologies
- [Fix] Fixed spill point calculation issue

Version 1.5.2
- Improvement to spill point calculation and debug gizmo

Version 1.5.1
- [Fix] Noise texture is no longer saved in the scene which can increase its size when using many prefabs

Version 1.5
- Added analytical volume computation for more precise liquid surface calculation (video: https://youtu.be/l4Zu6neUqr8)
- API: added MoveToLiquidSurface. This method can approximate displacement and rotation caused by turbulences. New demo scene "Buoyancy" (video: https://youtu.be/33lAPeyqwTY)
- Minimum Unity version upped to 2017.4

Version 1.4.1
- [Fix] Fixed build issue when floating point buffers are enabled

Version 1.4
- New option in Shader Features section to disable floating point buffers (improves compatibility with old mobile devices)

Version 1.3.2
- Improvements to irregular topology rendering
- Halved number of shader variants for multiple and multiple no flask styles

Version 1.3.1
- [Fix] Fixed issue when setting layers using scripting was producing incorrect liquid blending

Version 1.3:
- Added UpdateLayers(true) to force immediate refresh
- Improved beaker multiple layer demo
- Check for camera inside the liquid now accounts for near clip plane
- Reduced shader compilation time
- [Fix] Fixes to Bake Rotation and Center Pivot for complex objects


Version 1.2.3:
- [Fix] Fixed issue with multiple layers when Ignore Gravity is enabled and flask is rotated

Version 1.2.2:
- Added option to inspector to force Legacy rendering

Version 1.2.1:
- Unity 2018.3 support
- [Fix] Fixed irregular issue when using non-uniform scaling and scale.x is less than scale.z

Version 1.2:
- Added Bubbles seed option to inspector

Version 1.1:
- Animated light effects



License information
-------------------
- Liquid Volume developed by Ramiro Oliva.
- If purchased on the Asset Store, this asset is subjet to Asset Store EULA (https://unity3d.com/es/legal/as_terms). 
- If purchased on other site, Liqud Volume is licensed under specific terms by you and (C) 2018 Ramiro Oliva
- Uses LiquidVolumeFX.MIConvexHull library (MIT license) for the Fix Mesh feature