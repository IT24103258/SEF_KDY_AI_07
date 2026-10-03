# Local Development & Setup Guide

## Step 1: Clone & Configure Environment
```bash
git clone https://github.com/SLIIT-SE3090-2026/FixFlow-AI.git
cd FixFlow-AI
cp .env.example .env
```

## Step 2: Spin Up PostgreSQL Container
```bash
docker-compose up -d
```

## Step 3: Run ASP.NET Core API
```bash
cd backend/FixFlow.Api
dotnet restore
dotnet ef database update
dotnet run
```

## Step 4: Run React Frontend
```bash
cd frontend/fixflow-web
npm install
npm run dev
```

## Step 5: Run Python Agentic AI Microservice
```bash
cd agentic-ai
python -m venv venv
venv\Scripts\activate # Windows
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

> `main:app` is the unified entry point — it exposes `POST /api/classify`
> (Component 1) *and* the orchestrator router's `POST /api/orchestrator/execute`
> (Component 2). Starting `workflows.orchestrator:app` instead serves only the
> orchestrator and the backend receives **404** from `/api/classify`.
