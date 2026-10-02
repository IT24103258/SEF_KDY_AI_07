import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/routes/app_router.dart';
import 'core/theme/app_theme.dart';
import 'providers/auth_provider.dart';
import 'providers/theme_provider.dart';
// MEMBER 1
import 'providers/request_provider.dart';
import 'screens/login_screen.dart';

void main() {
  runApp(const FixFlowApp());
}

class FixFlowApp extends StatelessWidget {
  const FixFlowApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()..tryAutoLogin()),
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
        // MEMBER 1 — Request Intake & Classification
        ChangeNotifierProvider(create: (_) => RequestProvider()),
      ],
      child: Consumer2<AuthProvider, ThemeProvider>(
        builder: (context, authProvider, themeProvider, _) {
          if (authProvider.isRestoring) {
            return MaterialApp(
              debugShowCheckedModeBanner: false,
              theme: AppTheme.lightTheme,
              darkTheme: AppTheme.darkTheme,
              themeMode: themeProvider.themeMode,
              home: const Scaffold(
                body: Center(child: CircularProgressIndicator()),
              ),
            );
          }

          Widget initialScreen;
          if (authProvider.isAuthenticated) {
            final role = authProvider.user?.role;
            if (role != null) {
              if (role == 'Technician') {
                initialScreen = const LoginScreen(); 
              } else if (role == 'Requester') {
                initialScreen = const LoginScreen(); 
              } else {
                initialScreen = const LoginScreen();
              }
            } else {
              initialScreen = const LoginScreen();
            }
          } else {
            initialScreen = const LoginScreen();
          }

          return MaterialApp(
            title: 'FixFlow AI Mobile',
            debugShowCheckedModeBanner: false,
            theme: AppTheme.lightTheme,
            darkTheme: AppTheme.darkTheme,
            themeMode: themeProvider.themeMode,
            home: initialScreen,
            onGenerateRoute: AppRouter.generateRoute,
          );
        },
      ),
    );
  }
}