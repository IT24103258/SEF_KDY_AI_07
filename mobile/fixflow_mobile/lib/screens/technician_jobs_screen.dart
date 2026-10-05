import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:http/http.dart' as http;
import '../core/routes/app_router.dart';
import '../providers/auth_provider.dart';

class TechnicianJobsScreen extends StatefulWidget {
  const TechnicianJobsScreen({super.key});

  @override
  State<TechnicianJobsScreen> createState() => _TechnicianJobsScreenState();
}

class _TechnicianJobsScreenState extends State<TechnicianJobsScreen> {
  List<dynamic> assignedJobs = [];
  bool isLoading = true;

  final String csharpBaseUrl = 'http://localhost:5000/api/technicians';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      fetchMyAssignedJobs();
    });
  }

  Future<void> fetchMyAssignedJobs() async {
    final authProviderProvider = Provider.of<AuthProvider>(context, listen: false);
    final String? technicianEmail = authProviderProvider.user?.email;
    final emailToUse = technicianEmail ?? 'tech@fixflow.local';

    print("Fetching assigned jobs dynamically for email: $emailToUse");

    setState(() => isLoading = true);
    try {
      final response = await http.get(
        Uri.parse('$csharpBaseUrl/my-jobs?email=$emailToUse'),
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

  Future<void> _updateJobStatusOnServer(dynamic jobId, String newStatus, {String? reason}) async {
    try {
      final url = Uri.parse('$csharpBaseUrl/update-status');
      final bodyData = {
        "jobId": jobId,
        "status": newStatus,
        if (reason != null) "rejectionReason": reason,
      };

      print("Sending status update to backend: $bodyData");

      final response = await http.put(
        url,
        headers: {"Content-Type": "application/json"},
        body: jsonEncode(bodyData),
      );

      if (response.statusCode == 200 || response.statusCode == 204) {
        print("Job status successfully updated on backend.");
      } else {
        print("Failed to update status on server. Code: ${response.statusCode}");
      }
    } catch (e) {
      print("Error updating status on server: $e");
    }
  }

  // Pre-work Safety Checklist Dialog
  void _showSafetyChecklistDialog(BuildContext context, int index, dynamic job) {
    bool check1 = false; // PPE Worn
    bool check2 = false; // Safe area / hazard check
    bool check3 = false; // Tools verified
    final jobId = job["assignmentId"] ?? job["id"] ?? job["requestId"];

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: const Text('Pre-Work Safety Checklist'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Please confirm safety checks before starting work:'),
                  const SizedBox(height: 10),
                  CheckboxListTile(
                    title: const Text('Required PPE (Helmet, Gloves, etc.) worn'),
                    value: check1,
                    onChanged: (val) => setDialogState(() => check1 = val ?? false),
                  ),
                  CheckboxListTile(
                    title: const Text('Work area hazard inspection completed'),
                    value: check2,
                    onChanged: (val) => setDialogState(() => check2 = val ?? false),
                  ),
                  CheckboxListTile(
                    title: const Text('Tools and equipment safety verified'),
                    value: check3,
                    onChanged: (val) => setDialogState(() => check3 = val ?? false),
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Cancel'),
                ),
                ElevatedButton(
                  onPressed: (check1 && check2 && check3)
                      ? () async {
                          Navigator.pop(context);
                          
                          // Local UI update
                          setState(() {
                            assignedJobs[index]["status"] = "In Progress";
                          });

                          // Backend API call to save status permanently
                          await _updateJobStatusOnServer(jobId, "In Progress");

                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('Job started successfully!')),
                          );
                        }
                      : null, // Disabled unless all checked
                  style: ElevatedButton.styleFrom(backgroundColor: Colors.blue, foregroundColor: Colors.white),
                  child: const Text('Confirm & Start Job'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  // Job Rejection with a Reason Dialog
  void _showRejectJobDialog(BuildContext context, int index, dynamic job) {
    final TextEditingController reasonController = TextEditingController();
    final jobId = job["assignmentId"] ?? job["id"] ?? job["requestId"];

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Reject Assigned Job'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Please provide a valid reason for rejecting this job:'),
              const SizedBox(height: 10),
              TextField(
                controller: reasonController,
                decoration: const InputDecoration(
                  hintText: 'Enter reason (e.g. Lacks specialized skill, Parts unavailable)',
                  border: OutlineInputBorder(),
                ),
                maxLines: 3,
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
            ElevatedButton(
              onPressed: () async {
                final reason = reasonController.text.trim();
                if (reason.isEmpty) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Please enter a rejection reason!')),
                  );
                  return;
                }
                Navigator.pop(context);

                // Local UI update
                setState(() {
                  assignedJobs[index]["status"] = "Rejected";
                });

                // Backend API call to save rejection permanently
                await _updateJobStatusOnServer(jobId, "Rejected", reason: reason);

                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Job rejected with reason successfully.')),
                );
              },
              style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
              child: const Text('Submit Rejection'),
            ),
          ],
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final currentEmail = authProvider.user?.email ?? 'tech@fixflow.local';

    return Scaffold(
      appBar: AppBar(
        title: Text('My Jobs ($currentEmail)'),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: fetchMyAssignedJobs,
          )
        ],
      ),
      body: Column(
        children: [
          // Component 4 — Scheduling & Work Order Management Access
          Padding(
            padding: const EdgeInsets.all(12.0),
            child: Card(
              color: const Color(0xFF2457C5).withOpacity(0.08),
              child: ListTile(
                leading: const Icon(Icons.calendar_view_week, color: Color(0xFF2457C5), size: 32),
                title: const Text('Scheduling & Work Orders', style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: const Text('View schedule and manage all work orders'),
                trailing: const Icon(Icons.arrow_forward_ios, size: 16),
                onTap: () {
                  Navigator.pushNamed(context, AppRouter.component4Shell);
                },
              ),
            ),
          ),
          // Member 3 — Existing Job List
          Expanded(
            child: isLoading
                ? const Center(child: CircularProgressIndicator())
                : assignedJobs.isEmpty
                    ? const Center(child: Text('No assigned jobs available.'))
                    : Padding(
                        padding: const EdgeInsets.all(12.0),
                        child: ListView.builder(
                          itemCount: assignedJobs.length,
                          itemBuilder: (context, index) {
                            final job = assignedJobs[index];

                            final id = job["id"] ?? job["requestId"] ?? (index + 1);
                            final title = job["title"] ?? job["name"] ?? job["requiredSkill"] ?? "Maintenance Task";
                            final location = job["location"] ?? job["address"] ?? "Building A";
                            final priority = job["priority"] ?? job["priorityLevel"] ?? "High";
                            final status = job["status"] ?? job["Status"] ?? "Assigned";

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
                                          label: Text(status),
                                          backgroundColor: status == "Completed"
                                              ? Colors.green.shade100
                                              : status == "In Progress"
                                                  ? Colors.blue.shade100
                                                  : status == "Rejected"
                                                      ? Colors.red.shade100
                                                      : Colors.amber.shade100,
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 4),
                                    Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
                                    const SizedBox(height: 6),
                                    Text('Location: $location'),
                                    Text('Priority: $priority', style: const TextStyle(color: Colors.redAccent, fontWeight: FontWeight.bold)),
                                    const Divider(),
                                    
                                    if (status == "Assigned")
                                      Wrap(
                                        alignment: WrapAlignment.end,
                                        spacing: 8.0,
                                        children: [
                                          ElevatedButton(
                                            onPressed: () => _showSafetyChecklistDialog(context, index, job),
                                            style: ElevatedButton.styleFrom(backgroundColor: Colors.blue, foregroundColor: Colors.white),
                                            child: const Text('Accept / Start'),
                                          ),
                                          ElevatedButton(
                                            onPressed: () => _showRejectJobDialog(context, index, job),
                                            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
                                            child: const Text('Reject Job'),
                                          ),
                                        ],
                                      ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}