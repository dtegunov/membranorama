# In a nutshell
![Membranorama screenshot](https://dl.dropboxusercontent.com/u/14045247/membranorama.png)


#Membranorama – create and analyze panoramic views of biological membranes in electron tomograms.


## You will need
- Windows PC with a good GPU, the latest drivers and [.NET Framework 4.6](https://www.microsoft.com/en-us/download/details.aspx?id=48130) installed.
- Tomographic volume in MRC format, preferably generated in [IMOD](http://bio3d.colorado.edu/imod/); it has to be small enough to fit into your GPU memory (a 4k tomogram probably won't fit).
- Segmentation surface in OBJ format, exported from a tool like [FEI's Amira](https://www.fei.com/software/amira-3d-for-life-sciences/).


## Getting started

Use Membranogram.exe from this repository's /bin/Release folder (all files need to be present), or compile it yourself using [Visual Studio 2015](https://www.visualstudio.com/en-us/visual-studio-homepage-vs.aspx) or newer.

Use the buttons under "Files" to load the tomogram and surface files. You can use the session buttons later to save or load the entire session, uncluding all analysis data.

If everything went fine, you should see your membrane surface with the tomogram projected onto it (i. e. a membranogram). If the surface stays blank, the most likely cause is an incorrect offset value in the MRC header. Make sure to use the same file you used for segmentation in Amira, or copy its offset into the header of the file you want to load (e. g. binned version).

Navigate the membranogram:
- Click and drag to **rotate**
- Scroll to **zoom**
- Mouse wheel click and drag to **pan**

The initial **intensity range** and **lighting** may not be what you want. Adjust them using the parameters under "Display". If the membranogram is too blurry, adjust the **sharpening** under "Tracing" (this will cost you some GPU resources).

One of the original ideas behind membranograms is to improve the signal-to-noise ratio by **averaging multiple shells** perpendicularly to the membrane. You can set this under "Tracing"; the default is not to average. The averaged shells start at the surface, and then go outwards. If you use multiple shells, you probably want to offset them backwards, so the middle shell is at the membrane – make the offset value -Nshells/2 for that. Depending on how bumpy your surface is, averaging and offsetting will cause a significant distortion. Keep that in mind when interpreting the image.

To quickly explore the tomogram space adjacent to the membrane, change the **offset** value under "Surface". This will **shift the entire membrane** perpendicularly to its surface, i. e. along its normals. Any bumps will be amplified with increasing offset, but the tomogram projection itself won't be disturbed.



## Surface points

Hold Ctrl and click on the membrane to **pick a point**. If you don't like the location, hold Ctrl and drag the point to **move it**. Points picked on an offset surface will keep that offset when you move the surface.

The point box contains a stick to indicate its **rotation within the surface plane**. If the surface feature has a clear orientation, you can use this rotation angle later for analysis. To rotate the box, hold Ctrl and scroll the mouse wheel while hovering over the box. Additionally hold Shift to rotate faster.

To **delete a point**, select it in the "Points in (...)" list and click remove.

**Select/unselect multiple points** in the list by holding Ctrl.

Points will be added to the currently selected point group. Select a different group under "Point Groups" to add points to it.

If you have multiple point groups, you can **move points between them** by selecting the points, right-clicking the list, and selecting the name of the group you want to move them to.

Points can be **exported** to a tab-delimited text file:
- PositionXYZ: Coordinates in Angstrom, including the offset from the MRC header
- VolumeXYZ: Coordinates in pixels within the tomogram
- Offset: Offset from the original surface
- mXX: Fields of a 3x3 matrix describing the point's rotation, with the Z axis being the surface normal

Similarly, points can be **imported** from tab-delimited text files with the same field order, or from other Membranorama sessions.

**Point groups** have several properties to help with the analysis:
- Name: Click it to start editing, confirm the edit by pressing Enter.
- Size: The box size in tomogram pixels.
- Color: The default is to have slightly transparent colors, but you can make them opaque by changing the alpha channel value.
- N: Number of points in the group.
- Visibility: Show/hide all points in a group.

Point group depiction:
- Box: Default, cube with an orientation stick.
- Model: You can load either an OBJ mesh, e. g. exported from [UCSF Chimera](https://www.cgl.ucsf.edu/chimera/); or let Membranorama create an isosurface from an MRC volume, e. g. a protein density refined in [Relion](http://www2.mrc-lmb.cam.ac.uk/relion/index.php/Main_Page).
- Local isosurface: Makes each point shape unique, representing the isosurface of the sub-tomogram within it. Use it to locally sample the density landscape around the points.



## Surface patches

You can analyse **local parts of the surface** and **flatten** them to get as close to a 2D image of the curved membrane as possible.

Hold Shift, click and drag over the membrane to **select individual mesh triangles**. Selected triangles will be filled with a pattern of purple unicorns.

Hold Alt, click and drag over the selected triangles to **deselect** them.

Use the buttons under "Selection" to **modify the triangle selection**. "Fill" and "Grow" use an angle threshold to determine when to stop growing the selection. The angle is between the current selection's mean, and the new triangles to be selected.

Once you are satisfied with the selection, click "Add from Selection" under "Surface Patches" to **create a new patch**.

Similarly to surface points, you can color/uncolor, and show/hide patches.

Click on the box-shaped button next to the visibility checkbox to **open the patch in a new window**.

In the patch window, you will find the same rendering and surface offset controls as for the main membranogram. Additionally, since the representation is supposed to be 2D, you can set the **exact Angstrom/pixel** value for the image.

**To make a curved membrane patch planar**, click "Start Planarization". There is a tradeoff between planarity and image distortion that is controlled by the shape preservation factor. Planarity and distortion are indicated in the statistics below. Remember to click "Stop Planarization" before you do anything else.

It is often useful to have **multiple patches in the same reference frame**, e. g. to compare protein distribution in adjacent membranes. **Lock the view** of one or more patches to the same master patch. Camera lock will keep the zoom level, camera position and window size the same. Position lock will try to align the membranes as well as possible.



## Bonus

Take **screenshots** using the aperture button in the window frame next to minimize.



## Authorship

Membranorama is being developed by Dimitry Tegunov ([tegunov@gmail.com](mailto:tegunov@gmail.com), currently in Patrick Cramer's lab at the Max Planck Institute for Biophysical Chemistry in Göttingen, Germany.