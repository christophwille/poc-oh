
CREATE PROCEDURE byroyalty @percentage int
AS
select au_id from titleauthor
where titleauthor.royaltyper = @percentage

