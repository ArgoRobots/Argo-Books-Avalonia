# Architecture Overview

Argo Books is a cross-platform desktop accounting application built with modern .NET technologies.

## Technology Stack

![Tech Stack](diagrams/architecture/tech-stack.svg)

| Layer | Technology | Description |
|-------|------------|-------------|
| **Platform** | [.NET 10](https://dotnet.microsoft.com/en-us/) | Core runtime and framework |
| **UI Framework** | [Avalonia](https://avaloniaui.net/) | Cross-platform XAML-based UI |
| **Charts** | [LiveCharts2](https://livecharts.dev/) | Interactive data visualization |
| **Rendering** | [SkiaSharp](https://github.com/mono/SkiaSharp) | 2D graphics engine |

## MVVM Architecture

The application follows the [Model-View-ViewModel (MVVM)](https://docs.avaloniaui.net/docs/concepts/the-mvvm-pattern/) pattern for clean separation of concerns.

![MVVM Pattern](diagrams/architecture/mvvm.svg)

- **View** - XAML UI definitions and controls
- **ViewModel** - Presentation logic and state management
- **Model** - Business entities and data structures

## Project Contents

| Project | Contents |
|---------|----------|
| **ArgoBooks** | Views, ViewModels, Controls, Modals, UI Services |
| **ArgoBooks.Core** | Models, Business Services, Data, Security, Platform |
| **ArgoBooks.Desktop** | Desktop entry point (Windows/macOS/Linux) |
| **ArgoBooks.Browser** | WebAssembly entry point |
| **ArgoBooks.Tests** | Unit tests (xUnit) |

## Design Principles

1. **MVVM Pattern** - Clear separation between Views, ViewModels, and Models
2. **Service-Oriented** - Business logic encapsulated in dedicated services
3. **Cross-Platform** - Single codebase targets Windows, macOS, Linux, and Web
4. **In-Memory Data** - Fast operations with full data loaded in memory
5. **File-Based Storage** - Portable `.argo` files instead of database
6. **Compiled Bindings** - Performance-optimized data binding
7. **Singleton Services** - App-wide service instances via DI
