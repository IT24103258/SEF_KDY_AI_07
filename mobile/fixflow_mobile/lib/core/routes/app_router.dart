import 'package:flutter/material.dart';
import '../../screens/login_screen.dart';
import '../../screens/main_shell_screen.dart';
import '../../screens/profile_screen.dart';
import '../../screens/notifications_screen.dart';
import '../../screens/job_details_screen.dart';
import '../../screens/job_execution_screen.dart';
import '../../screens/digital_signature_screen.dart';

class AppRouter {
  static const String login = '/login';
  static const String home = '/';
  static const String profile = '/profile';
  static const String notifications = '/notifications';

  static const String technicianSchedule = home;
  static const String allJobs = '/jobs';

  // Retained for callers that still use the original static routes.
  static const String jobDetails = '/job-details';
  static const String jobExecution = '/job-execution';
  static const String jobSignature = '/job-signature';

  static String jobDetailsPath(String workOrderId) =>
      '/jobs/${Uri.encodeComponent(workOrderId)}';

  static String jobExecutionPath(String workOrderId) =>
      '${jobDetailsPath(workOrderId)}/execute';

  static String jobSignaturePath(String workOrderId) =>
      '${jobDetailsPath(workOrderId)}/signature';

  static Route<dynamic> generateRoute(RouteSettings settings) {
    final routeName = settings.name ?? home;
    final uri = Uri.tryParse(routeName);
    final segments = uri?.pathSegments ?? const <String>[];

    if (segments.length == 2 && segments.first == 'jobs') {
      return MaterialPageRoute(
        settings: settings,
        builder: (_) => JobDetailsScreen(workOrderId: segments[1]),
      );
    }

    if (segments.length == 3 && segments.first == 'jobs') {
      final workOrderId = segments[1];
      if (segments[2] == 'execute') {
        return MaterialPageRoute(
          settings: settings,
          builder: (_) => JobExecutionScreen(workOrderId: workOrderId),
        );
      }
      if (segments[2] == 'signature') {
        return _signatureRoute(settings, workOrderId);
      }
    }

    switch (routeName) {
      case login:
        return MaterialPageRoute(builder: (_) => const LoginScreen());
      case home:
        return MaterialPageRoute(
            builder: (_) => const MainShellScreen(initialIndex: 0));
      case allJobs:
        return MaterialPageRoute(
            builder: (_) => const MainShellScreen(initialIndex: 1));
      case profile:
        return MaterialPageRoute(builder: (_) => const ProfileScreen());
      case notifications:
        return MaterialPageRoute(builder: (_) => const NotificationsScreen());
      case jobDetails:
        final id = settings.arguments as String? ?? '';
        return MaterialPageRoute(
            builder: (_) => JobDetailsScreen(workOrderId: id));
      case jobExecution:
        final id = settings.arguments as String? ?? '';
        return MaterialPageRoute(
            builder: (_) => JobExecutionScreen(workOrderId: id));
      case jobSignature:
        final args = settings.arguments as Map<String, dynamic>? ?? const {};
        return _signatureRoute(settings, args['workOrderId'] as String? ?? '');
      default:
        return MaterialPageRoute(
          builder: (_) => Scaffold(
            body: Center(child: Text('No route defined for $routeName')),
          ),
        );
    }
  }

  static MaterialPageRoute<dynamic> _signatureRoute(
    RouteSettings settings,
    String workOrderId,
  ) {
    final args = settings.arguments as Map<String, dynamic>? ?? const {};
    return MaterialPageRoute(
      settings: settings,
      builder: (_) => DigitalSignatureScreen(
        workOrderId: workOrderId,
        elapsedSeconds: args['elapsedSeconds'] as int? ?? 0,
        photoCount: args['photoCount'] as int? ?? 0,
        noteCount: args['noteCount'] as int? ?? 0,
        uploadedFileKey: args['uploadedFileKey'] as String?,
        uploadedPhotoName: args['uploadedPhotoName'] as String?,
      ),
    );
  }
}
