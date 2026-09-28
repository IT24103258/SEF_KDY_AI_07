import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/routes/app_router.dart';
import 'core/theme/app_theme.dart';
import 'providers/auth_provider.dart';
import 'providers/theme_provider.dart';
<<<<<<< HEAD
import 'providers/work_order_provider.dart';
=======
// MEMBER 1
import 'providers/request_provider.dart';

>>>>>>> main

void main() {
  runApp(const FixFlowApp());
}

class FixFlowApp extends StatelessWidget {
  const FixFlowApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
<<<<<<< HEAD
        ChangeNotifierProvider(create: (_) => WorkOrderProvider()),
=======
        // MEMBER 1 — Request Intake & Classification
        ChangeNotifierProvider(create: (_) => RequestProvider()),
>>>>>>> main
      ],
      child: Consumer<ThemeProvider>(
        builder: (context, themeProvider, child) {
          return MaterialApp(
            title: 'FixFlow AI Mobile',
            debugShowCheckedModeBanner: false,
            theme: AppTheme.lightTheme,
            darkTheme: AppTheme.darkTheme,
            themeMode: themeProvider.themeMode,
            initialRoute: AppRouter.login,
            onGenerateRoute: AppRouter.generateRoute,
          );
        },
      ),
    );
  }
}
