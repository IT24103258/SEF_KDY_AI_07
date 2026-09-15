class ApiConstants {
  // Default Android Emulator IP targeting localhost ASP.NET Core API Gateway
  static const String baseUrl = 'http://localhost:5000/api';
  
  static const String login = '/auth/login';
  static const String currentUser = '/auth/me';
  static const String locations = '/locations';
  static const String assets = '/assets';
  static const String notifications = '/notifications';
}
