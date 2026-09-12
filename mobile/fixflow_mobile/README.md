# FixFlow AI Mobile Application (Flutter)

> **Provider + ChangeNotifier State Management**

## Architecture & Gateway Rules
- Communicates **STRICTLY** with ASP.NET Core Web API (`http://10.0.2.2:5000/api` for Android emulator or `http://localhost:5000/api` for iOS simulator).
- **NEVER** connects directly to PostgreSQL or internal Python Agentic AI microservice.

## Shared Router File
Edit `lib/core/routes/app_router.dart` and add your route constants and screen routes under your clearly marked section (Member 1–4).
