[![Tilia logo][Tilia-Image]](#)

> ### Utilities -> Tilia Package Importer for the Unity Software
> A helper utility window for the Unity software to allow easy importing of Tilia packages

## Introduction

The Tilia Package Importer allows easy import of the [Tilia] packages into the [Unity] software as the default Unity Package Manager only supports the old `all` route for package listing which is no longer supported on npmjs.

## Getting Started

Browse to `Main Menu -> Window -> Tilia -> Package Importer` in the Unity software and select the `Package Importer` option.

This will open the `Package Importer` window.

If the `io.extendreality` Scoped Registry is not present in the Unity project manifest.json file then a message will appear promting to attempt to add the required scoped registry by clicking the `Add Scoped Registry` button.

When the `io.extendreality` Scoped Registry is present in the Unity project manifest.json file a list of available Tilia packages will be displayed in the `Package Importer` window.

Find the required package and click `Add` next to the relevant package to attempt to add that package to your project.

Clicking the `View` button will open the GitHub webpage for the relevant package.

You can filter the list contents by free typing into the `Filter` text box.

> If you need to refresh the package list, then click the `Refresh Package List` button.

## Code of Conduct

Please refer to the Extend Reality [Code of Conduct].

## Third Party Pacakges

The Tilia Package Importer uses the following 3rd party packages:

* [SimpleJSON] by Bunny83.

## License

Code released under the [MIT License][License].

[Tilia-Image]: https://raw.githubusercontent.com/ExtendRealityLtd/related-media/main/github/readme/tilia.png
[License]: LICENSE.md
[Code of Conduct]: https://github.com/ExtendRealityLtd/.github/blob/master/CODE_OF_CONDUCT.md

[Tilia]: https://www.vrtk.io/tilia.html
[Unity]: https://unity3d.com/
[SimpleJSON]: https://github.com/Bunny83/SimpleJSON