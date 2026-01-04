# Argo Books

## Introduction

**Argo Books** is easy-to-use accounting software with receipt scanning, predictive analytics, inventory management, and more. Everything you need to run your business. Available for Windows, macOS, and Linux. It's available for download at www.argorobots.com.

## Features

- **AI Receipt Scanning**: Take a photo of any receipt with your phone or upload from your computer. Our AI automatically extracts vendor, date, amount, and line items with 99% accuracy.
- **Expense & Revenue Tracking**: An intuitive interface makes recording expenses and revenue simple. Smart input validation prevents errors before they happen.
- **Predictive Analytics**: Analyzes your historical data to forecast sales trends, identify seasonal patterns, and help you make data-driven decisions.
- **AI Business Insights**: Get intelligent suggestions to optimize your business. AI analyzes your data to identify opportunities, flag concerns, and recommend actions.
- **Inventory Management**: Track stock levels, set reorder points, and never run out of your best-selling items. Real-time visibility across all your products.
- **Rental Management**: Manage equipment rentals, track availability, and handle bookings with ease.
- **Customer Management**: Keep track of customer information, purchase history, and preferences.
- **Invoicing**: Create, send, and track invoices with ease. Customize templates, set payment terms, and get paid faster.
- **Online Payments**: Let customers pay invoices online with credit cards or bank transfers. Integrated with Stripe, PayPal, and Square.
- **Security**: AES-256 encryption protects all your data. Your data stays on your computer, not in the cloud. Biometric authentication support with Windows Hello.

## Technologies Used

- **[C# .NET 10](https://dotnet.microsoft.com/en-us/)**: Core framework for the application's logic and UI.
- **[Avalonia UI](https://avaloniaui.net/)**: Cross-platform UI framework for building native applications on Windows, macOS, and Linux.
- **[LiveCharts2](https://livecharts.dev/)**: Used to visualize data through beautiful and interactive charts.

## Prerequisites

- **.NET 10 SDK**: Make sure you have the latest .NET 10 SDK installed on your machine. You can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- **JetBrains Rider**: This is the IDE used for Avalonia UI. You can download it [here](https://www.jetbrains.com/rider/).
- **API Keys**: You'll need to obtain the .env file containing the API keys from Evan Di Placido.

## Installation

1. Clone the repository to your local machine.
2. Place the .env file in the project root directory (where the .sln file is located).
3. Build the project by pressing `Ctrl + Shift + B`.

## Running the Application

After building the solution, press F5 to run the application in Debug mode, or `Ctrl + F5` to run it in Release mode.

## Publishing

### Windows
The Windows `.exe` installer is built using [Advanced Installer Professional Edition](https://www.advancedinstaller.com/).

### macOS
The macOS `.dmg` installer is created using [create-dmg](https://github.com/create-dmg/create-dmg).

### Linux
The Linux distribution is packaged as an [AppImage](https://appimage.org/) using [appimagetool](https://github.com/AppImage/appimagetool).
