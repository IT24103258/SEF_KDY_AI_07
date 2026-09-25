import 'package:flutter/material.dart';
import '../../screens/login_screen.dart';
import '../../screens/main_shell_screen.dart';

import '../../screens/profile_screen.dart';
import '../../screens/notifications_screen.dart';
import '../../screens/job_details_screen.dart';
import '../../screens/job_execution_screen.dart';
import '../../screens/digital_signature_screen.dart';

/*
================================================================================
FIXFLOW SHARED FOUNDATION FLUTTER ROUTER
================================================================================
IMPORTANT FOR ALL 4 MEMBERS:
Add your route constants and route cases directly in your designated section below.
Do NOT create complex dynamic routing abstractions.
================================================================================
*/

class AppRouter {
  // ============================================================
  // SHARED ROUTE CONSTANTS
  // ============================================================
  static const String login = '/login';
  static const String home = '/';
  static const String profile = '/profile';
  static const String notifications = '/notifications';

  // ============================================================
  // MEMBER 1 ROUTE CONSTANTS — REQUEST INTAKE & CLASSIFICATION
  // ============================================================
  // Example: static const String requestIntake = '/request-intake';


  // ============================================================
  // MEMBER 2 ROUTE CONSTANTS — RISK & PRIORITY ASSESSMENT
  // ============================================================
  // Example: static const String priorityDetails = '/priority-details';


  // ============================================================
  // MEMBER 3 ROUTE CONSTANTS — TECHNICIAN MATCHING & ASSIGNMENT
  // ============================================================
  // Example: static const String technicianJobList = '/technician-jobs';


  // ============================================================
  // MEMBER 4 ROUTE CONSTANTS — SCHEDULING & WORK ORDER MANAGEMENT
  // ============================================================
  static const String technicianSchedule = '/technician-schedule';
  static const String jobDetails = '/job-details';
  static const String jobExecution = '/job-execution';
  static const String jobSignature = '/job-signature';
  static const String allJobs = '/all-jobs';


  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      // Shared Foundation Route Cases
      case login:
        return MaterialPageRoute(builder: (_) => const LoginScreen());
      case home:
        return MaterialPageRoute(builder: (_) => const MainShellScreen());
      case profile:
        return MaterialPageRoute(builder: (_) => const ProfileScreen());
      case notifications:
        return MaterialPageRoute(builder: (_) => const NotificationsScreen());

      // ============================================================
      // MEMBER 1 ROUTE CASES — REQUEST INTAKE & CLASSIFICATION
      // ============================================================
      // case requestIntake:
      //   return MaterialPageRoute(builder: (_) => const RequestIntakeScreen());


      // ============================================================
      // MEMBER 2 ROUTE CASES — RISK & PRIORITY ASSESSMENT
      // ============================================================
      // case priorityDetails:
      //   return MaterialPageRoute(builder: (_) => const PriorityDetailsScreen());


      // ============================================================
      // MEMBER 3 ROUTE CASES — TECHNICIAN MATCHING & ASSIGNMENT
      // ============================================================
      // case technicianJobList:
      //   return MaterialPageRoute(builder: (_) => const TechnicianJobListScreen());


      // ============================================================
      // MEMBER 4 ROUTE CASES — SCHEDULING & WORK ORDER MANAGEMENT
      // ============================================================
      case technicianSchedule:
        // Redirect to home shell which hosts the schedule as tab 0
        return MaterialPageRoute(builder: (_) => const MainShellScreen(initialIndex: 0));
      case allJobs:
        // Redirect to home shell which hosts All Jobs as tab 1
        return MaterialPageRoute(builder: (_) => const MainShellScreen(initialIndex: 1));
      case jobDetails:
        final id = settings.arguments as String? ?? '';
        return MaterialPageRoute(builder: (_) => JobDetailsScreen(workOrderId: id));
      case jobExecution:
        final id = settings.arguments as String? ?? '';
        return MaterialPageRoute(builder: (_) => JobExecutionScreen(workOrderId: id));
      case jobSignature:
        final args = settings.arguments as Map<String, dynamic>? ?? {};
        return MaterialPageRoute(
          builder: (_) => DigitalSignatureScreen(
            workOrderId: args['workOrderId'] as String? ?? '',
            elapsedSeconds: args['elapsedSeconds'] as int? ?? 0,
            photoCount: args['photoCount'] as int? ?? 0,
            noteCount: args['noteCount'] as int? ?? 0,
            uploadedFileKey: args['uploadedFileKey'] as String?,
          ),
        );


      default:
        return MaterialPageRoute(
          builder: (_) => Scaffold(
            body: Center(
              child: Text('No route defined for ${settings.name}'),
            ),
          ),
        );
    }
  }
}
