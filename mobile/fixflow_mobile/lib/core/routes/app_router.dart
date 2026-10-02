import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';

import '../../screens/login_screen.dart';
import '../../screens/home_screen.dart';
import '../../screens/profile_screen.dart';
import '../../screens/notifications_screen.dart';
import '../../screens/technician_jobs_screen.dart';

// ── MEMBER 1 ─────────────────────────────────────────────────────────────────
import '../../screens/requests/submit_request_screen.dart';
import '../../screens/requests/edit_request_screen.dart';
import '../../screens/requests/my_requests_screen.dart';
import '../../screens/requests/request_detail_screen.dart';

// ── MEMBER 2 ─────────────────────────────────────────────────────────────────
import '../../screens/priority_details_screen.dart';
import '../../screens/risk_matrix_screen.dart';
import '../../screens/risk_simulator_screen.dart';

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
  static const String submitRequest = '/submit-request';
  static const String editRequest   = '/edit-request';
  static const String myRequests    = '/my-requests';
  static const String requestDetail = '/request-detail';


  // ============================================================
  // MEMBER 2 ROUTE CONSTANTS — RISK & PRIORITY ASSESSMENT
  // ============================================================
  static const String priorityDetails = '/priority-details';
  static const String riskMatrix = '/risk-matrix';
  static const String riskSimulator = '/risk-simulator';


  // ============================================================
  // MEMBER 3 ROUTE CONSTANTS — TECHNICIAN MATCHING & ASSIGNMENT
  // ============================================================
  static const String technicianHome = '/technician-home';

  static String homeRouteFor(String role) {
    switch (role.toLowerCase()) {
      case 'technician':
        return technicianHome;
      case 'requester':
      default:
        return home;
    }
  }

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
      case submitRequest:
        return MaterialPageRoute(
            builder: (_) => const SubmitRequestScreen());
      case editRequest:
        return MaterialPageRoute(
            settings: settings,
            builder: (_) => const EditRequestScreen());
      case myRequests:
        return MaterialPageRoute(
            builder: (_) => const MyRequestsScreen());
      case requestDetail:
        return MaterialPageRoute(
            settings: settings,
            builder: (_) => const RequestDetailScreen());

      // ============================================================
      // MEMBER 2 ROUTE CASES — RISK & PRIORITY ASSESSMENT
      // ============================================================
      case priorityDetails:
        return MaterialPageRoute(builder: (_) => const PriorityDetailsScreen());

      case riskMatrix:
        return MaterialPageRoute(builder: (_) => const RiskMatrixScreen());

      case riskSimulator:
        final args = settings.arguments as Map<String, dynamic>? ?? {};
        return MaterialPageRoute(
          builder: (_) => RiskSimulatorScreen(
            requestId: args['requestId'] ?? '',
            requestNumber: args['requestNumber'] ?? '',
            initialCriticality: args['assetCriticality'],
            initialImpact: args['impactLevel'],
            initialLikelihood: args['likelihoodLevel'],
          ),
        );


      // ============================================================
      // MEMBER 3 ROUTE CASES — TECHNICIAN MATCHING & ASSIGNMENT
      // ============================================================
      case technicianHome:
        return MaterialPageRoute(builder: (_) => const TechnicianJobsScreen());


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

/// Mirrors React's <ProtectedRoute>:
///   - unauthenticated user → login screen
///   - authenticated user without an allowed role → their own home route
/// Screens stay registered in the router — access is guarded, never removed.
/// The backend [Authorize] attributes remain the authoritative check.
class _RouteGuard extends StatefulWidget {
  final Set<String>? allowedRoles; // null = any authenticated user
  final Widget child;

  const _RouteGuard({this.allowedRoles, required this.child});

  @override
  State<_RouteGuard> createState() => _RouteGuardState();
}

class _RouteGuardState extends State<_RouteGuard> {
  bool _redirecting = false;

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    String? redirect;
    if (!auth.isAuthenticated) {
      redirect = AppRouter.login;
    } else if (auth.user == null || auth.user?.role == null) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    } else if (widget.allowedRoles != null &&
        !widget.allowedRoles!.contains(auth.user!.role)) {
      redirect = AppRouter.homeRouteFor(auth.user!.role);
    }

    if (redirect != null) {
      if (!_redirecting) {
        _redirecting = true;
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) {
            Navigator.of(context)
                .pushNamedAndRemoveUntil(redirect!, (_) => false);
          }
        });
      }
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    _redirecting = false;
    return widget.child;
  }
}