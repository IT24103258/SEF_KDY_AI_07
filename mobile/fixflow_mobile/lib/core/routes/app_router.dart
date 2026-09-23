import 'package:flutter/material.dart';
import '../../screens/login_screen.dart';
import '../../screens/home_screen.dart';
import '../../screens/profile_screen.dart';
import '../../screens/notifications_screen.dart';
import '../../screens/priority_details_screen.dart';

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
  static const String priorityDetails = '/priority-details';


  // ============================================================
  // MEMBER 3 ROUTE CONSTANTS — TECHNICIAN MATCHING & ASSIGNMENT
  // ============================================================
  // Example: static const String technicianJobList = '/technician-jobs';


  // ============================================================
  // MEMBER 4 ROUTE CONSTANTS — SCHEDULING & WORK ORDER MANAGEMENT
  // ============================================================
  // Example: static const String workOrderChecklist = '/work-order-checklist';


  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      // Shared Foundation Route Cases
      case login:
        return MaterialPageRoute(builder: (_) => const LoginScreen());
      case home:
        return MaterialPageRoute(builder: (_) => const HomeScreen());
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
      case priorityDetails:
        return MaterialPageRoute(builder: (_) => const PriorityDetailsScreen());


      // ============================================================
      // MEMBER 3 ROUTE CASES — TECHNICIAN MATCHING & ASSIGNMENT
      // ============================================================
      // case technicianJobList:
      //   return MaterialPageRoute(builder: (_) => const TechnicianJobListScreen());


      // ============================================================
      // MEMBER 4 ROUTE CASES — SCHEDULING & WORK ORDER MANAGEMENT
      // ============================================================
      // case workOrderChecklist:
      //   return MaterialPageRoute(builder: (_) => const WorkOrderChecklistScreen());


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
