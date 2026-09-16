import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/theme_provider.dart';
import '../core/routes/app_router.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final themeProvider = Provider.of<ThemeProvider>(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('FixFlow Mobile Dashboard'),
        actions: [
          IconButton(
            icon: Icon(themeProvider.isDarkMode ? Icons.light_mode : Icons.dark_mode),
            onPressed: () => themeProvider.toggleTheme(),
          ),
          IconButton(
            icon: const Icon(Icons.person),
            onPressed: () => Navigator.pushNamed(context, AppRouter.profile),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: ListTile(
                leading: const CircleAvatar(
                  backgroundColor: Color(0xFF2563EB),
                  child: Icon(Icons.person, color: Colors.white),
                ),
                title: Text(
                  'Welcome, ${authProvider.user?.firstName ?? 'Technician'}!',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                subtitle: Text('Role: ${authProvider.user?.role ?? 'Technician'}'),
              ),
            ),
            const SizedBox(height: 20),

            // Technician Quick Access Card
            Card(
              color: const Color(0xFF2563EB).withOpacity(0.08),
              child: ListTile(
                leading: const Icon(Icons.build_circle, color: Color(0xFF2563EB), size: 36),
                title: const Text('My Assigned Jobs', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('View and update assigned maintenance tasks'),
                trailing: const Icon(Icons.arrow_forward_ios, size: 16),
                onTap: () {
                  Navigator.pushNamed(context, AppRouter.technicianHome);
                },
              ),
            ),

            const SizedBox(height: 24),
            const Text(
              'Mobile Workflows Gateway',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            const Card(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'API Gateway Connection: Active',
                      style: TextStyle(fontWeight: FontWeight.bold, color: Colors.green),
                    ),
                    SizedBox(height: 8),
                    Text(
                      'This Flutter mobile app connects exclusively to ASP.NET Core Web API. Direct database or Python agent access is strictly prohibited.',
                      style: TextStyle(fontSize: 12, color: Colors.grey),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}