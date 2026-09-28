import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class TechnicianJobsScreen extends StatefulWidget {
  const TechnicianJobsScreen({super.key});

  @override
  State<TechnicianJobsScreen> createState() => _TechnicianJobsScreenState();
}

class _TechnicianJobsScreenState extends State<TechnicianJobsScreen> {
  List<dynamic> assignedJobs = [];
  bool isLoading = true;

  // Localhost URL for Flutter Web (Port 5000)
  final String csharpBaseUrl = 'http://localhost:5000/api/technicians';

  @override
  void initState() {
    super.initState();
    fetchMyAssignedJobs();
  }

  Future<void> fetchMyAssignedJobs() async {
    setState(() => isLoading = true);
    try {
      final response = await http.get(
        Uri.parse('$csharpBaseUrl/my-jobs?email=tech@fixflow.com'),
      );

      print("Response Status: ${response.statusCode}");
      print("Response Body: ${response.body}");

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        setState(() {
          assignedJobs = data;
          isLoading = false;
        });
      } else {
        setState(() => isLoading = false);
      }
    } catch (e) {
      print("Fetch Error: $e");
      setState(() => isLoading = false);
    }
  }

  void _updateStatus(int index, String newStatus) {
    setState(() {
      assignedJobs[index]["status"] = newStatus;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Job #${assignedJobs[index]["id"]} updated to $newStatus')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Assigned Jobs'),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: fetchMyAssignedJobs,
          )
        ],
      ),
      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : assignedJobs.isEmpty
              ? const Center(child: Text('No assigned jobs available.'))
              : Padding(
                  padding: const EdgeInsets.all(12.0),
                  child: ListView.builder(
                    itemCount: assignedJobs.length,
                    itemBuilder: (context, index) {
                      final job = assignedJobs[index];

                      // Dynamic keys fallback with explicit integer handling
                      final id = job["id"] ?? job["requestId"] ?? (index + 1);
                      final title = job["title"] ?? job["name"] ?? job["requiredSkill"] ?? "Maintenance Task";
                      final location = job["location"] ?? job["address"] ?? "Building A";
                      final priority = job["priority"] ?? job["priorityLevel"] ?? "High";
                      final status = job["status"] ?? (job["isAvailable"] == false ? "Assigned" : "In Progress");

                      return Card(
                        margin: const EdgeInsets.symmetric(vertical: 8),
                        child: Padding(
                          padding: const EdgeInsets.all(14.0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('Job #$id', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                                  Chip(
                                    label: Text('$status'),
                                    backgroundColor: status == "Completed" ? Colors.green.shade100 : Colors.amber.shade100,
                                  ),
                                ],
                              ),
                              const SizedBox(height: 4),
                              Text('$title', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
                              const SizedBox(height: 6),
                              Text('Location: $location'),
                              Text('Priority: $priority', style: const TextStyle(color: Colors.redAccent, fontWeight: FontWeight.bold)),
                              const Divider(),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.end,
                                children: [
                                  if (status != "Completed")
                                    ElevatedButton(
                                      onPressed: () => _updateStatus(index, "Completed"),
                                      style: ElevatedButton.styleFrom(backgroundColor: Colors.green, foregroundColor: Colors.white),
                                      child: const Text('Mark Completed'),
                                    ),
                                ],
                              )
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}