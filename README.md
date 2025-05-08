*Because DocFX's built-in markdown is not very good*

If it wasn't clear from the title, this tool converts docfx metadata to [Astro Starlight](https://starlight.astro.build/) markdown.

The generated markdown aims to be as close to the official Microsoft documentation as possible.

This project is a mess currently (and may forever be) but it works for the most part.

## Usage

Use [docfx](https://dotnet.github.io/docfx/) to generate metadata.

```bash
docfx metadata
```

Then use docfx2astro to convert the metadata to markdown.

```bash
docfx2astro -i <path to docfx metadata> -o <path to output markdown>
```

## Installation

```bash
dotnet tool install -g hertzole.docfx2astro
```