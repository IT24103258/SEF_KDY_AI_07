class ApiConstants {
  // Default Android Emulator IP targeting localhost ASP.NET Core API Gateway
  static const String baseUrl = 'http://localhost:5000/api';
  
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String currentUser = '/auth/me';
  static const String locations = '/locations';
  static const String assets = '/assets';
  static const String notifications = '/notifications';

  // ── MEMBER 1 — Request Intake & Classification ──────────────────────────
  static const String requests          = '/requests';
  static const String myRequests        = '/requests/me';
  static const String issueCategories   = '/issue-categories';
  // Detail, classify & override use the id at runtime: '/requests/$id', etc.
}
