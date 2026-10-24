UPDATE AspNetUsers
SET TrainerId = (SELECT Id FROM AspNetUsers WHERE Email = 'trainer1@test.com')
WHERE Email = 'client1@test.com';